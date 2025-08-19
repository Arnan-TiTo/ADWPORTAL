using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using miniApp.API.Data;
using System;
using System.Collections.Generic;
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

        public SqlController(AppDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        private bool IsAuthorized()
        {
            var expectedToken = _config["AUTH_TOKEN"];
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                return false;

            var token = authHeader.Substring("Bearer ".Length).Trim();
            return token == expectedToken;
        }

        [HttpPost("excCmd")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> ExecuteSqlFile(IFormFile file, [FromHeader] string username)
        {
            if (!IsAuthorized()) return Unauthorized();

            var userId = await _context.Users
                           .Where(u => u.Username == username)
                           .Select(u => u.Id)
                           .FirstOrDefaultAsync();

            if (userId == 0)
                return Unauthorized(new { error = "Invalid username" });

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            string sqlText;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                sqlText = await reader.ReadToEndAsync();
            }

            string responseMsg;
            bool success = false;

            try
            {
                if (sqlText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    var conn = _context.Database.GetDbConnection();
                    await conn.OpenAsync();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = sqlText;
                    using var readerDb = await cmd.ExecuteReaderAsync();

                    var result = new List<Dictionary<string, object>>();
                    while (await readerDb.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < readerDb.FieldCount; i++)
                        {
                            row[readerDb.GetName(i)] = readerDb.IsDBNull(i) ? null : readerDb.GetValue(i);
                        }
                        result.Add(row);
                    }

                    responseMsg = "Success (SELECT)";
                    success = true;

                    // เก็บ log
                    await SaveLog(userId, sqlText, responseMsg);

                    return Ok(result);
                }
                else
                {
                    await _context.Database.ExecuteSqlRawAsync(sqlText);
                    responseMsg = "Success (Non-SELECT)";
                    success = true;

                    await SaveLog(userId, sqlText, responseMsg);

                    return Ok(new { message = responseMsg });
                }
            }
            catch (SqlException ex)
            {
                responseMsg = "Fail: " + ex.Message;
                await SaveLog(userId, sqlText, responseMsg);
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task SaveLog(int userId, string sqlText, string response)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "INSERT INTO ExcCommandLog (UserId, SqlCommand, Response) VALUES (@uid, @sql, @res)",
                new SqlParameter("@uid", userId),
                new SqlParameter("@sql", sqlText),
                new SqlParameter("@res", response)
            );
        }
    }
}
