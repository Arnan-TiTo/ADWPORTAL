using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using miniApp.API.Data;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace miniApp.API.SqlControllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SqlController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        // ทำให้สร้างตาราง log แค่ครั้งเดียวต่อโปรเซส แต่ retry ถ้ารอบก่อนล้มเหลว
        private static bool _logTableEnsured;
        private static readonly object _logLock = new();

        public SqlController(AppDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ===== Auth แบบ Bearer token ใน appsettings: AUTH_TOKEN =====
        private bool IsAuthorized()
        {
            var expectedToken = _config["AUTH_TOKEN"];
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.Ordinal))
                return false;

            var token = authHeader.Substring("Bearer ".Length).Trim();
            return token == expectedToken;
        }

        // ===== สร้างตาราง log ถ้ายังไม่มี (dbo.ExcCommandLog) =====
        private async Task EnsureLogTableAsync(ILogger logger)
        {
            if (_logTableEnsured) return;
            // double-checked locking
            lock (_logLock)
            {
                if (_logTableEnsured) return;
            }

            try
            {
                await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[ExcCommandLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ExcCommandLog](
        [Id]         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId]     INT NOT NULL,
        [SqlCommand] NVARCHAR(MAX) COLLATE Thai_CI_AI NOT NULL,
        [Response]   NVARCHAR(MAX) COLLATE Thai_CI_AI NOT NULL,
        [CreatedAt]  DATETIME2(0) NOT NULL
            CONSTRAINT [DF_ExcCommandLog_CreatedAt] DEFAULT SYSUTCDATETIME()
    );
END
");
                lock (_logLock) _logTableEnsured = true; // ✅ set true เฉพาะเมื่อสร้างสำเร็จ
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "EnsureLogTable failed");
                lock (_logLock) _logTableEnsured = false; // ❗ ให้ retry ได้ในรอบถัดไป
            }
        }

        [HttpPost("excCmd")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> ExecuteSqlFile(
            IFormFile file,
            [FromHeader] string username,
            [FromServices] ILogger<SqlController> logger)
        {
            if (!IsAuthorized()) return Unauthorized();

            var userId = await _context.Users
                .Where(u => u.Username == username)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userId == 0)
                return Unauthorized(new { error = "Invalid username" });

            if (file is null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            string sqlText;
            using (var sr = new StreamReader(file.OpenReadStream()))
                sqlText = await sr.ReadToEndAsync();

            try
            {
                // ปรับ timeout เผื่อสคริปต์ยาว
                _context.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

                // ตัดช่องว่างขึ้นต้น
                var trimmed = sqlText.TrimStart();

                if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    await using var conn = _context.Database.GetDbConnection();
                    if (conn.State != System.Data.ConnectionState.Open)
                        await conn.OpenAsync();

                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = sqlText;

                    var result = new List<Dictionary<string, object?>>();
                    await using var rdr = await cmd.ExecuteReaderAsync();

                    while (await rdr.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>(rdr.FieldCount, StringComparer.OrdinalIgnoreCase);
                        for (int i = 0; i < rdr.FieldCount; i++)
                            row[rdr.GetName(i)] = await rdr.IsDBNullAsync(i) ? null : rdr.GetValue(i);
                        result.Add(row);
                    }

                    var saved = await TrySaveLog(userId, sqlText, "Success (SELECT)", logger);
                    Response.Headers["x-log-saved"] = saved ? "true" : "false";
                    return Ok(result);
                }
                else
                {
                    await _context.Database.ExecuteSqlRawAsync(sqlText);

                    var saved = await TrySaveLog(userId, sqlText, "Success (EXECUTE)", logger);
                    Response.Headers["x-log-saved"] = saved ? "true" : "false";
                    return Ok(new { message = "Success (EXECUTE)" });
                }
            }
            // EF Core มัก wrap error DB เป็น DbUpdateException
            catch (DbUpdateException ex) when (ex.InnerException is DbException dbEx)
            {
                logger.LogError(ex, "DB error executing script");
                var msg = dbEx.Message;
                await TrySaveLog(userId, sqlText, "Fail: " + msg, logger);

                if (dbEx is SqlException se)
                    return BadRequest(new { error = msg, code = se.Number, state = se.State, line = se.LineNumber });

                return BadRequest(new { error = msg });
            }
            // Provider exception โดยตรง (SqlException, NpgsqlException, ฯลฯ)
            catch (DbException ex)
            {
                logger.LogError(ex, "DB error executing script");
                await TrySaveLog(userId, sqlText, "Fail: " + ex.Message, logger);

                if (ex is SqlException se)
                    return BadRequest(new { error = se.Message, code = se.Number, state = se.State, line = se.LineNumber });

                return BadRequest(new { error = ex.Message });
            }
            // เผื่อกรณีอื่น ๆ
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error executing script");
                await TrySaveLog(userId, sqlText, "Fail: " + ex.Message, logger);
                return StatusCode(500, new { error = ex.GetBaseException().Message });
            }
        }

        // ===== บันทึก Log (พร้อม ensure ตาราง) =====
        private async Task<bool> TrySaveLog(int userId, string sqlText, string response, ILogger logger)
        {
            try
            {
                await EnsureLogTableAsync(logger);

                var pUser = new SqlParameter("@uid", userId);

                // ใช้ NVARCHAR(MAX) => size -1 สำหรับพารามิเตอร์
                var pSql = new SqlParameter("@sql", System.Data.SqlDbType.NVarChar, -1)
                { Value = (object?)sqlText ?? DBNull.Value };

                var pRes = new SqlParameter("@res", System.Data.SqlDbType.NVarChar, -1)
                { Value = (object?)response ?? DBNull.Value };

                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO [dbo].[ExcCommandLog] (UserId, SqlCommand, Response) VALUES (@uid, @sql, @res)",
                    pUser, pSql, pRes);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SaveLog failed");
                return false;
            }
        }
    }
}
