using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using miniApp.API.Data;
using miniApp.API.Dtos;
using miniApp.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace miniApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductStockController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public ProductStockController(AppDbContext context, IConfiguration config)
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

        private async Task SetUserSessionContextAsync(int userId)
        {
            if (userId <= 0) return;
            var p = new SqlParameter("@userId", userId);
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sys.sp_set_session_context @key=N'user_id', @value=@userId", p);
        }

        // ===== Helpers =====
        private async Task<int> ResolveWarehouseHeadLocationIdAsync(int userId)
        {
            var headFromUserLoc = await (
                from ul in _context.UserLocations
                join l in _context.Locations on ul.LocationId equals l.Id
                where ul.UserId == userId
                      && l.isStorehouse == 1
                select l.Id
            ).FirstOrDefaultAsync();

            if (headFromUserLoc > 0) 
                return headFromUserLoc;
            else
                return headFromUserLoc > 0 ? headFromUserLoc : 0;
        }

        private async Task<int> ResolveWarehouseHeadLocationIdReturnDamageToWarehouseAsync(int userId)
        {
            var headGlobal = await (
                from l in _context.Locations 
                where l.isWarehouse == 1
                select l.Id
            ).FirstOrDefaultAsync();

            if (headGlobal > 0) 
                return headGlobal;
            else
                return headGlobal > 0 ? headGlobal : 0;
        }

        private async Task EnsureStockRowAsync(int productId, int locationId)
        {
            var exists = await _context.ProductStocks
                .AnyAsync(x => x.ProductId == productId && x.LocationId == locationId);
            if (!exists)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO dbo.ProductStocks
                      (ProductId, LocationId, QtyOnHand, QtyReserved, QtyDamaged,
                       MinLevel, MaxLevel, ReorderPoint, Cost, UpdatedAt, QtyReceive)
                    VALUES
                      ({productId}, {locationId}, 0, 0, 0, NULL, NULL, NULL, 0, SYSUTCDATETIME(), 0);");
            }
        }
        private static string MakeIdemRefId(
        string reason, int productId, int locationId, int qty, int? userId, string? salt = null)
        {
            var raw = $"{reason}|-P{productId}|-L{locationId}|-Q{qty}|-U{(userId ?? 0)}|{salt ?? ""}";
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            var hex = Convert.ToHexString(hash).Substring(0, 8);
            return $"API-{hex}";
        }

        // ====== GET: api/productstock (paged) ======
        [HttpGet]
        public async Task<IActionResult> GetStocks([FromQuery] int userId, [FromQuery] int? locationId,
                                                   [FromQuery] int? productId, [FromQuery] string? search,
                                                   [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (userId <= 0) return BadRequest("userId is required.");

            await SetUserSessionContextAsync(userId);

            var q = from ps in _context.ProductStocks
                    join p in _context.Products on ps.ProductId equals p.Id
                    join l in _context.Locations on ps.LocationId equals l.Id
                    select new
                    {
                        ps.ProductId,
                        p.Name,
                        p.Sku,
                        p.ImageUrl,
                        ps.LocationId,
                        LocationName = l.Name,
                        ps.QtyOnHand,
                        ps.QtyReserved,
                        ps.QtyDamaged,
                        ps.QtyAvailable,
                        ps.QtyReceive,
                        l.isWarehouse,
                        l.isStorehouse,
                        l.isDamagehouse,
                        p.Price,
                        TotalQtyAllLocations = p.Quantity,
                        CategoryName = (string?)null,
                        BrandName = (string?)null
                    };

            if (locationId.HasValue) q = q.Where(x => x.LocationId == locationId.Value);
            if (productId.HasValue) q = q.Where(x => x.ProductId == productId.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(x => (x.Name ?? "").Contains(s) || (x.Sku ?? "").Contains(s) || (x.LocationName ?? "").Contains(s));
            }

            var total = await q.CountAsync();
            var items = await q.OrderBy(x => x.Name).ThenBy(x => x.LocationName)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        [HttpGet("location/{locationId:int}")]
        public async Task<ActionResult<IEnumerable<ProductStockRowDto>>> GetByLocation(int locationId, [FromQuery] int? userId)
        {
            var rows = await (from s in _context.ProductStocks
                              join p in _context.Products on s.ProductId equals p.Id
                              where s.LocationId == locationId
                              orderby p.Name
                              select new ProductStockRowDto
                              {
                                  ProductId = p.Id,
                                  Name = p.Name,
                                  Sku = p.Sku,
                                  ImageUrl = p.ImageUrl,
                                  Price = (decimal?)p.Price ?? 0,
                                  QtyOnHand = s.QtyOnHand,
                                  QtyReserved = s.QtyReserved,
                                  QtyDamaged = s.QtyDamaged,
                                  QtyAvailable = s.QtyAvailable
                              }).ToListAsync();

            return Ok(rows);
        }

        [HttpGet("dropdown-not-in-location")]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> DropdownNotInLocation(
            [FromQuery] int locationId, [FromQuery] string? q = null, [FromQuery] int top = 50)
        {
            if (locationId <= 0) return Ok(Array.Empty<ProductResponseDto>());

            var query = _context.Products
                .Where(p => !_context.ProductStocks
                    .Any(s => s.LocationId == locationId && s.ProductId == p.Id));

            if (!string.IsNullOrWhiteSpace(q))
            {
                var s = q.Trim();
                query = query.Where(p => p.Name.Contains(s) || p.Sku.Contains(s));
            }

            var list = await query
                .OrderBy(p => p.Name)
                .Take(top)
                .Select(p => new ProductResponseDto { Id = p.Id, Name = p.Name, Sku = p.Sku })
                .ToListAsync();

            return Ok(list);
        }


        // ====== GET: api/productstock/product/{productId}?userId=... ======
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProduct(int productId, [FromQuery] int userId)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (userId <= 0) return BadRequest("userId is required.");

            await SetUserSessionContextAsync(userId);

            var q = from ps in _context.ProductStocks
                    join p in _context.Products on ps.ProductId equals p.Id
                    join l in _context.Locations on ps.LocationId equals l.Id
                    where ps.ProductId == productId
                    select new
                    {
                        ps.ProductId,
                        p.Name,
                        p.Sku,
                        p.ImageUrl,
                        ps.LocationId,
                        LocationName = l.Name,
                        ps.QtyOnHand,
                        ps.QtyReserved,
                        ps.QtyDamaged,
                        ps.QtyAvailable,
                        ps.QtyReceive,
                        l.isWarehouse,
                        l.isStorehouse,
                        l.isDamagehouse,
                        p.Price,
                        TotalQtyAllLocations = p.Quantity,
                        CategoryName = (string?)null,
                        BrandName = (string?)null
                    };

            var items = await q.OrderBy(x => x.LocationName).ToListAsync();
            return Ok(items);
        }

        // ====== GET: api/productstock/location/{locationId}?userId=... ======
        [HttpGet("location/{locationId}")]
        public async Task<IActionResult> GetByLocation(int locationId, [FromQuery] int userId)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (userId <= 0) return BadRequest("userId is required.");

            await SetUserSessionContextAsync(userId);

            var q = from ps in _context.ProductStocks
                    join p in _context.Products on ps.ProductId equals p.Id
                    join l in _context.Locations on ps.LocationId equals l.Id
                    where ps.LocationId == locationId
                    select new
                    {
                        ps.ProductId,
                        p.Name,
                        p.Sku,
                        p.ImageUrl,
                        ps.LocationId,
                        LocationName = l.Name,
                        ps.QtyOnHand,
                        ps.QtyReserved,
                        ps.QtyDamaged,
                        ps.QtyAvailable,
                        ps.QtyReceive,
                        l.isWarehouse,
                        l.isStorehouse,
                        l.isDamagehouse,            // NEW
                        p.Price,
                        TotalQtyAllLocations = p.Quantity,
                        CategoryName = (string?)null,
                        BrandName = (string?)null
                    };

            var items = await q.OrderBy(x => x.Name).ToListAsync();
            return Ok(items);
        }

        // ====== POST: api/productstock/adjust ======
        private static readonly HashSet<string> OUT_REASONS =
            new(StringComparer.OrdinalIgnoreCase)
            { "ISSUE","ADJUST_OUT","ADJUSTOUT","SALE","SO","DAMAGE_WRITE_OFF","CYCLE_COUNT_SHORT","SCRAP","WASTE","MANUALOUT" };

        private static readonly HashSet<string> IN_REASONS =
            new(StringComparer.OrdinalIgnoreCase)
            { "PURCHASE","PO","ADJUST_IN","ADJUSTIN","INITIAL","RETURN","CYCLE_COUNT_GAIN","MANUALIN" };

        [HttpPost("adjust")]
        public async Task<IActionResult> Adjust([FromBody] AdjustStockDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand == 0)
                return BadRequest("ProductId, LocationId, Qty are required.");

            await SetUserSessionContextAsync(dto.PerformedByUserId ?? 0);

            var rawReason = (dto.ReasonCode ?? "").Trim();
            string normReason;
            bool isIn;

            if (IN_REASONS.Contains(rawReason)) { isIn = true; normReason = rawReason.ToUpperInvariant(); }
            else if (OUT_REASONS.Contains(rawReason)) { isIn = false; normReason = rawReason.ToUpperInvariant(); }
            else { isIn = dto.QtyOnHand > 0; normReason = isIn ? "PURCHASE" : "ISSUE"; }

            var qtyAbs = Math.Abs(dto.QtyOnHand);
            if (qtyAbs <= 0) return BadRequest("Qty must be non-zero.");

            if (isIn && normReason.Equals("ADJUST_IN", StringComparison.OrdinalIgnoreCase))
            {
                var flags = await _context.Locations
                    .Where(x => x.Id == dto.LocationId)
                    .Select(x => new { x.isStorehouse, x.isWarehouse })
                    .FirstOrDefaultAsync();

                if (flags == null) return NotFound("Location not found.");
            }

            await using var tx = await _context.Database.BeginTransactionAsync();

            var p = new[]
            {
                new SqlParameter("@ProductId", dto.ProductId),
                new SqlParameter("@FromLocationId", isIn ? (object)DBNull.Value : dto.LocationId),
                new SqlParameter("@ToLocationId",   isIn ? dto.LocationId : (object)DBNull.Value),
                new SqlParameter("@Qty", qtyAbs),
                new SqlParameter("@ReasonCode", normReason),
                new SqlParameter("@ReferenceType", (object?)dto.ReferenceType ?? DBNull.Value),
                new SqlParameter("@ReferenceId",   (object?)dto.ReferenceId   ?? DBNull.Value),
                new SqlParameter("@PerformedByUserId", (object?)dto.PerformedByUserId ?? DBNull.Value),
                new SqlParameter("@Note", (object?)dto.Note ?? DBNull.Value),
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_AdjustOrTransferStock " +
                "@ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, " +
                "@ReferenceType, @ReferenceId, @PerformedByUserId, @Note", p);

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/transfer ======
        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferStockDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.FromLocationId <= 0 || dto.ToLocationId <= 0 || dto.QtyOnHand <= 0)
                return BadRequest("ProductId, FromLocationId, ToLocationId, positive Qty are required.");
            if (dto.FromLocationId == dto.ToLocationId) return BadRequest("From/To cannot be the same.");
            if (string.IsNullOrWhiteSpace(dto.ReasonCode)) dto.ReasonCode = "TRANSFER";

            await using var tx = await _context.Database.BeginTransactionAsync();

            await EnsureStockRowAsync(dto.ProductId, dto.ToLocationId);

            var p = new[]
            {
                new SqlParameter("@ProductId", dto.ProductId),
                new SqlParameter("@FromLocationId", dto.FromLocationId),
                new SqlParameter("@ToLocationId", dto.ToLocationId),
                new SqlParameter("@Qty", dto.QtyOnHand),
                new SqlParameter("@ReasonCode", dto.ReasonCode),
                new SqlParameter("@ReferenceType", (object?)dto.ReferenceType ?? DBNull.Value),
                new SqlParameter("@ReferenceId", (object?)dto.ReferenceId ?? DBNull.Value),
                new SqlParameter("@PerformedByUserId", (object?)dto.PerformedByUserId ?? DBNull.Value),
                new SqlParameter("@Note", (object?)dto.Note ?? DBNull.Value),
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_AdjustOrTransferStock @ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note", p);

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/reserve ======
        [HttpPost("reserve")]
        public async Task<IActionResult> Reserve([FromBody] ReserveDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0)
                return BadRequest("ProductId, LocationId, positive Qty are required.");

            await using var tx = await _context.Database.BeginTransactionAsync();

            var affected = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE dbo.ProductStocks
                   SET QtyReserved = QtyReserved + {2}, UpdatedAt = SYSUTCDATETIME()
                 WHERE ProductId = {0} AND LocationId = {1}
                   AND (QtyOnHand - QtyReserved - QtyDamaged) >= {2};",
                dto.ProductId, dto.LocationId, dto.QtyOnHand);

            if (affected == 0)
            {
                await tx.RollbackAsync();
                return Conflict("Insufficient available quantity to reserve.");
            }

            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO dbo.StockTransactions
                    (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES
                    ({0}, {1}, NULL, {2}, 'RESERVE', {3}, {4}, {5}, {6});",
                dto.ProductId, dto.LocationId, dto.QtyOnHand,
                (object?)dto.ReferenceType ?? DBNull.Value,
                (object?)dto.ReferenceId ?? DBNull.Value,
                (object?)dto.PerformedByUserId ?? DBNull.Value,
                (object?)dto.Note ?? DBNull.Value);

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/release-reserve ======
        [HttpPost("release-reserve")]
        public async Task<IActionResult> ReleaseReserve([FromBody] ReserveDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0)
                return BadRequest("ProductId, LocationId, positive Qty are required.");

            await using var tx = await _context.Database.BeginTransactionAsync();

            var affected = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE dbo.ProductStocks
                   SET QtyReserved = QtyReserved - {2}, UpdatedAt = SYSUTCDATETIME()
                 WHERE ProductId = {0} AND LocationId = {1}
                   AND QtyReserved >= {2};",
                dto.ProductId, dto.LocationId, dto.QtyOnHand);

            if (affected == 0)
            {
                await tx.RollbackAsync();
                return Conflict("Not enough reserved quantity to release.");
            }

            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO dbo.StockTransactions
                    (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES
                    ({0}, {1}, NULL, {2}, 'RESERVE_CANCEL', {3}, {4}, {5}, {6});",
                dto.ProductId, dto.LocationId, dto.QtyOnHand,
                (object?)dto.ReferenceType ?? DBNull.Value,
                (object?)dto.ReferenceId ?? DBNull.Value,
                (object?)dto.PerformedByUserId ?? DBNull.Value,
                (object?)dto.Note ?? DBNull.Value);

            await tx.CommitAsync();
            return NoContent();
        }

        [HttpPost("ship-from-reserved")]
        public async Task<IActionResult> ShipFromReserved([FromBody] ReserveDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0)
                return BadRequest("ProductId, LocationId, positive Qty are required.");

            await SetUserSessionContextAsync(dto.PerformedByUserId ?? 0);

            await using var tx = await _context.Database.BeginTransactionAsync();

            var affected = await _context.Database.ExecuteSqlRawAsync(@"
            UPDATE dbo.ProductStocks
            SET QtyReserved = QtyReserved - {2}, UpdatedAt = SYSUTCDATETIME()
            WHERE ProductId = {0} AND LocationId = {1}
            AND QtyReserved >= {2};",
            dto.ProductId, dto.LocationId, dto.QtyOnHand);

            if (affected == 0)
            {
                await tx.RollbackAsync();
                return Conflict("Not enough reserved quantity to ship.");
            }

            var p = new[]
            {
                new SqlParameter("@ProductId", dto.ProductId),
                new SqlParameter("@FromLocationId", dto.LocationId),
                new SqlParameter("@ToLocationId", DBNull.Value),
                new SqlParameter("@Qty", dto.QtyOnHand),
                new SqlParameter("@ReasonCode", "SALE"),
                new SqlParameter("@ReferenceType", (object?)dto.ReferenceType ?? DBNull.Value),
                new SqlParameter("@ReferenceId", (object?)dto.ReferenceId ?? DBNull.Value),
                new SqlParameter("@PerformedByUserId", (object?)dto.PerformedByUserId ?? DBNull.Value),
                new SqlParameter("@Note", (object?)dto.Note ?? DBNull.Value),
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_AdjustOrTransferStock @ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note", p);

            await tx.CommitAsync();
            return NoContent();
        }


        [HttpPost("damage-add")]
        public async Task<IActionResult> DamageAdd([FromBody] DamageDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0)
                return BadRequest("ProductId, LocationId, positive Qty are required.");

            await SetUserSessionContextAsync(dto.PerformedByUserId ?? 0);

            const string reason = "DAMAGE_ADD";
            var refType = string.IsNullOrWhiteSpace(dto.ReferenceType) ? "API_DAMAGE" : dto.ReferenceType!.Trim();

            var refId = string.IsNullOrWhiteSpace(dto.ReferenceId)
                ? MakeIdemRefId(reason, dto.ProductId, dto.LocationId, dto.QtyOnHand, dto.PerformedByUserId, dto.Note)
                : dto.ReferenceId!.Trim();

            await using var tx = await _context.Database.BeginTransactionAsync();

            // SP จัดการอัปเดต + ลง log + เช็คกันซ้ำ
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_AdjustOrTransferStock " +
                "@ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, " +
                "@ReferenceType, @ReferenceId, @PerformedByUserId, @Note",
                new SqlParameter("@ProductId", dto.ProductId),
                new SqlParameter("@FromLocationId", dto.LocationId),
                new SqlParameter("@ToLocationId", DBNull.Value),
                new SqlParameter("@Qty", dto.QtyOnHand),
                new SqlParameter("@ReasonCode", reason),
                new SqlParameter("@ReferenceType", (object)refType),
                new SqlParameter("@ReferenceId", (object)refId),
                new SqlParameter("@PerformedByUserId", (object?)dto.PerformedByUserId ?? DBNull.Value),
                new SqlParameter("@Note", (object?)dto.Note ?? DBNull.Value)
            );

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/damage-repair ======
        [HttpPost("damage-repair")]
        public async Task<IActionResult> DamageRepair([FromBody] DamageDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0)
                return BadRequest("ProductId, LocationId, positive Qty are required.");

            await using var tx = await _context.Database.BeginTransactionAsync();

            var affected = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE dbo.ProductStocks
                   SET QtyDamaged = QtyDamaged - {2}, UpdatedAt = SYSUTCDATETIME()
                 WHERE ProductId = {0} AND LocationId = {1}
                   AND QtyDamaged >= {2};",
                dto.ProductId, dto.LocationId, dto.QtyOnHand);

            if (affected == 0)
            {
                await tx.RollbackAsync();
                return Conflict("Not enough damaged quantity to repair.");
            }

            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO dbo.StockTransactions
                    (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES
                    ({0}, {1}, NULL, {2}, 'DAMAGE_REPAIR', {3}, {4}, {5}, {6});",
                dto.ProductId, dto.LocationId, dto.QtyOnHand,
                (object?)dto.ReferenceType ?? DBNull.Value,
                (object?)dto.ReferenceId ?? DBNull.Value,
                (object?)dto.PerformedByUserId ?? DBNull.Value,
                (object?)dto.Note ?? DBNull.Value);

            await tx.CommitAsync();
            return NoContent();
        }


        [HttpGet("audit")]
        public async Task<IActionResult> Audit([FromQuery] int locationId, [FromQuery] int productId, [FromQuery] int top = 50)
        {
        if (!IsAuthorized()) return Unauthorized();
        if (locationId <= 0 || productId <= 0) return BadRequest("locationId and productId are required.");

        top = Math.Clamp(top, 1, 500);

        try
        {
            await using var conn = (SqlConnection)_context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

                const string sql = @"
                    SELECT TOP(@Top)
                        st.CreatedAt,
                        st.ReasonCode AS Action,
                        CASE
                            WHEN st.ReasonCode IN('RESERVE','RESERVE_CANCEL','RESERVE_SHIP') THEN 0
                            WHEN st.FromLocationId = @LocationId AND st.ToLocationId IS NULL THEN -ABS(st.QtyChange)
                            WHEN st.FromLocationId IS NULL AND st.ToLocationId = @LocationId THEN + ABS(st.QtyChange)
                            WHEN st.FromLocationId = @LocationId AND st.ToLocationId IS NOT NULL THEN - ABS(st.QtyChange)
                            WHEN st.ToLocationId = @LocationId AND st.FromLocationId IS NOT NULL THEN + ABS(st.QtyChange)
                            ELSE 0
                        END AS MovementQty,
                        CASE
                            WHEN st.ReasonCode = 'RESERVE'        THEN + ABS(st.QtyChange)
                            WHEN st.ReasonCode = 'RESERVE_CANCEL' THEN - ABS(st.QtyChange)
                            ELSE 0
                        END AS ReserveDelta,
                        st.Note,
                        COALESCE(NULLIF(u.Fullname, ''), NULLIF(u.Username, ''), '(system)') AS ByUser
                    FROM dbo.StockTransactions st
                    LEFT JOIN dbo.Users u ON st.PerformedByUserId = u.Id
                    WHERE st.ProductId = @ProductId
                      AND(st.FromLocationId = @LocationId
                           OR st.ToLocationId = @LocationId
                           OR(st.FromLocationId IS NULL AND st.ToLocationId IS NULL))
                    ORDER BY st.CreatedAt DESC;
                ";

                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add(new SqlParameter("@Top", top));
                cmd.Parameters.Add(new SqlParameter("@ProductId", productId));
                cmd.Parameters.Add(new SqlParameter("@LocationId", locationId));

                var list = new List<object>();
                await using var rd = await cmd.ExecuteReaderAsync();

                int iCreatedAt = rd.GetOrdinal("CreatedAt");
                int iAction = rd.GetOrdinal("Action");
                int iMovementQty = rd.GetOrdinal("MovementQty");
                int iReserveDelta = rd.GetOrdinal("ReserveDelta");
                int iNote = rd.GetOrdinal("Note");
                int iByUser = rd.GetOrdinal("ByUser");

                while (await rd.ReadAsync())
                {
                    var movement = rd.IsDBNull(iMovementQty) ? 0 : rd.GetInt32(iMovementQty);
                    var reserve = rd.IsDBNull(iReserveDelta) ? 0 : rd.GetInt32(iReserveDelta);

                    list.Add(new
                    {
                        CreatedAt = rd.GetDateTime(iCreatedAt),
                        Action = rd.IsDBNull(iAction) ? "" : rd.GetString(iAction),
                        Qty = movement,
                        ReserveDelta = reserve, 
                        Note = rd.IsDBNull(iNote) ? null : rd.GetString(iNote),
                        ByUser = rd.IsDBNull(iByUser) ? "(system)" : rd.GetString(iByUser)
                    });
                }
            return Ok(list);
        }
        catch (SqlException ex)
        {
            return StatusCode(500, new
            {
                message = "Audit failed (SqlException)",
                error = ex.Message,
                ex.Number,
                ex.State,
                ex.Class,
                ex.Procedure,
                ex.LineNumber,
                ex.Server,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (DbUpdateException ex)
        {
            var root = ex.GetBaseException();
            return StatusCode(500, new
            {
                message = "Audit failed (DbUpdateException)",
                error = root?.Message ?? ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Audit failed (Exception)",
                error = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    // ====== POST: api/productstock/add-row ======
    [HttpPost("add-row")]
        public async Task<IActionResult> AddRow([FromBody] AddStockRowDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto is null) return BadRequest("Body is required.");
            if (dto.ProductId <= 0 || dto.LocationId <= 0)
                return BadRequest("ProductId, LocationId are required.");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var exists = await _context.ProductStocks
                    .AnyAsync(x => x.ProductId == dto.ProductId && x.LocationId == dto.LocationId);
                if (exists) return Conflict("Row already exists for this product & location.");

                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO dbo.ProductStocks
                      (ProductId, LocationId, QtyOnHand, QtyReserved, QtyDamaged,
                       MinLevel, MaxLevel, ReorderPoint, Cost, UpdatedAt)
                    VALUES
                      ({dto.ProductId}, {dto.LocationId}, 0, 0, 0, NULL, NULL, NULL, 0, SYSUTCDATETIME());");

                if (dto.InitialQty > 0)
                {
                    var p = new[]
                    {
                        new SqlParameter("@ProductId", dto.ProductId),
                        new SqlParameter("@FromLocationId", DBNull.Value),
                        new SqlParameter("@ToLocationId", dto.LocationId),
                        new SqlParameter("@Qty", dto.InitialQty),
                        new SqlParameter("@ReasonCode", "INITIAL"),
                        new SqlParameter("@ReferenceType", (object?)(dto.ReferenceType ?? "INITIAL")),
                        new SqlParameter("@ReferenceId", (object?)dto.ReferenceId ?? DBNull.Value),
                        new SqlParameter("@PerformedByUserId", (object?)dto.PerformedByUserId ?? DBNull.Value),
                        new SqlParameter("@Note", (object?)dto.Note ?? DBNull.Value),
                    };

                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC dbo.sp_AdjustOrTransferStock @ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note", p);
                }

                await tx.CommitAsync();
                return StatusCode(201, new { message = "Created", dto.ProductId, dto.LocationId });
            }
            catch (SqlException ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new
                {
                    message = "Add-row failed (SqlException)",
                    error = ex.Message,
                    ex.Number,
                    ex.State,
                    ex.Procedure,
                    ex.LineNumber,
                    ex.Server
                });
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync();
                var root = ex.GetBaseException();
                return StatusCode(500, new
                {
                    message = "Add-row failed (DbUpdateException)",
                    error = root?.Message ?? ex.Message
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "Add-row failed (Exception)", error = ex.Message });
            }
        }

        // ====== DELETE: api/productstock/{locationId}/{productId} ======
        [HttpDelete("{locationId:int}/{productId:int}")]
        public async Task<IActionResult> DeleteRow(int locationId, int productId)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (locationId <= 0 || productId <= 0) return BadRequest();

            try
            {
                var row = await _context.ProductStocks
                    .Where(x => x.LocationId == locationId && x.ProductId == productId)
                    .Select(x => new { x.QtyOnHand, x.QtyReserved, x.QtyDamaged })
                    .FirstOrDefaultAsync();

                if (row == null) return NotFound();

                if (row.QtyOnHand != 0 || row.QtyReserved != 0 || row.QtyDamaged != 0)
                    return Conflict(new
                    {
                        message = "Cannot delete. Quantities must be zero.",
                        row.QtyOnHand,
                        row.QtyReserved,
                        row.QtyDamaged
                    });

                var affected = await _context.Database.ExecuteSqlRawAsync(
                    @"DELETE FROM dbo.ProductStocks WHERE LocationId = {0} AND ProductId = {1};",
                    locationId, productId);

                if (affected == 0) return NotFound();
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(409, new { message = "Delete failed due to DB constraint.", error = ex.GetBaseException().Message });
            }
            catch (SqlException ex)
            {
                return StatusCode(409, new { message = "Delete failed due to DB constraint/trigger.", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error while deleting row.", error = ex.Message });
            }
        }

        // ====== GET: api/productstock/warehousehead/mine ======
        [HttpGet("warehousehead/mine")]
        public async Task<IActionResult> MyWarehouseHead([FromQuery] int userId)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (userId <= 0) return BadRequest("userId is required.");

            var headId = await ResolveWarehouseHeadLocationIdAsync(userId);
            if (headId <= 0) return NotFound("WarehouseHead not found.");

            var name = await _context.Locations.Where(l => l.Id == headId).Select(l => l.Name).FirstOrDefaultAsync() ?? "";
            return Ok(new { locationId = headId, name });
        }

        // ====== POST: api/productstock/issue-from-head ======
        [HttpPost("issue-from-head")]
        public async Task<IActionResult> IssueFromHead([FromBody] IssueFromHeadDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto is null || dto.ProductId <= 0 || dto.ToLocationId <= 0 || dto.Qty <= 0)
                return BadRequest();

            await SetUserSessionContextAsync(dto.PerformedByUserId ?? 0);

            await using var tx = await _context.Database.BeginTransactionAsync();

            var headId = await ResolveWarehouseHeadLocationIdAsync(dto.PerformedByUserId ?? 0);
            if (headId <= 0) return NotFound("WarehouseHead not found.");

            await EnsureStockRowAsync(dto.ProductId, headId);
            await EnsureStockRowAsync(dto.ProductId, dto.ToLocationId);

            var p = new[]
            {
                new SqlParameter("@ProductId", dto.ProductId),
                new SqlParameter("@FromLocationId", headId),
                new SqlParameter("@ToLocationId", dto.ToLocationId),
                new SqlParameter("@Qty", dto.Qty),
                new SqlParameter("@ReasonCode", "TRANSFER"),
                new SqlParameter("@ReferenceType", (object?)dto.ReferenceType ?? DBNull.Value),
                new SqlParameter("@ReferenceId", (object?)dto.ReferenceId ?? DBNull.Value),
                new SqlParameter("@PerformedByUserId", (object?)dto.PerformedByUserId ?? DBNull.Value),
                new SqlParameter("@Note", (object?)dto.Note ?? DBNull.Value),
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_AdjustOrTransferStock @ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note", p);

            await tx.CommitAsync();
            return StatusCode(201, new { message = "Issued from head." });
        }

        private static string NewDocNo() => $"DR{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        // ====== POST: api/productstock/damage-return/request ======// 
        // สร้างเอกสาร PENDING + DocNo และเพิ่ม QtyReceive ที่ Warehouse ======//
        [HttpPost("damage-return/request")]
        public async Task<IActionResult> DamageReturnRequest([FromBody] DamageReturnCreateDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto is null || dto.ProductId <= 0 || dto.FromLocationId <= 0 || dto.Qty <= 0)
                return BadRequest();

            await SetUserSessionContextAsync(dto.PerformedByUserId ?? 0);

            // 1) ตรวจต้นทางต้องเป็น storehouse
            var fromLoc = await _context.Locations
                .Where(x => x.Id == dto.FromLocationId)
                .Select(x => new { x.Id, x.isStorehouse })
                .FirstOrDefaultAsync();
            if (fromLoc is null) return NotFound("From location not found.");
            if (fromLoc.isStorehouse != 1) return BadRequest("From location must be a storehouse.");

            // 2) กำหนดปลายทาง: ใช้ที่ส่งมา ถ้าไม่มีก็หา WarehouseHead
            var toId = dto.ToLocationId;
            if (toId <= 0)
            {
                toId = await ResolveWarehouseHeadLocationIdReturnDamageToWarehouseAsync(dto.PerformedByUserId ?? 0);
                if (toId <= 0) return NotFound("WarehouseHead not found.");
            }

            // 2.1 ยืนยันว่า To คือคลังใหญ่จริง (isWarehouse == 1 และไม่ใช่ storehouse/damagehouse)
            var toFlags = await _context.Locations
                .Where(l => l.Id == toId)
                .Select(l => new { l.isWarehouse, l.isStorehouse, l.isDamagehouse, l.Name })
                .FirstOrDefaultAsync();
            if (toFlags is null) return NotFound("To location not found.");
            if (toFlags.isWarehouse != 1 || (toFlags.isStorehouse ?? 0) == 1 || (toFlags.isDamagehouse ?? 0) == 1)
                return BadRequest("To location must be a Warehouse (not Storehouse/Damagehouse).");

            await using var tx = await _context.Database.BeginTransactionAsync();

            // 3) สร้างเอกสาร (ยังไม่ตัด OnHand/ Damaged ใดๆ)
            var doc = new DamageReturnDocs
            {
                DocNo = NewDocNo(),
                ProductId = dto.ProductId,
                FromLocationId = dto.FromLocationId,
                ToLocationId = toId,
                Qty = dto.Qty,
                Status = "PENDING",
                Note = dto.Note,
                CreatedByUserId = dto.PerformedByUserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.DamageReturnDocs.Add(doc);
            await _context.SaveChangesAsync(); // get doc.Id

            // 4) เพิ่ม QtyReceive ให้ Warehouse ปลายทาง
            await EnsureStockRowAsync(dto.ProductId, toId);
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.ProductStocks
            SET QtyReceive = QtyReceive + {dto.Qty}, UpdatedAt = SYSUTCDATETIME()
            WHERE ProductId = {dto.ProductId} AND LocationId = {toId};");

            // บันทึกทรานแซกชันกำกับเหตุผล REQUEST
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.StockTransactions
            (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
            VALUES
            ({dto.ProductId}, {dto.FromLocationId}, {toId}, {dto.Qty}, 'DAMAGE_RETURN_REQUEST', N'DAMAGE_RETURN_DOC', {doc.Id}, {dto.PerformedByUserId}, {dto.Note});");

            await tx.CommitAsync();

            // ส่งคืนรายละเอียดเอกสารเพื่อให้ UI แสดงผลได้ทันที
            return StatusCode(201, new
            {
                id = doc.Id,
                docNo = doc.DocNo,
                productId = doc.ProductId,
                fromLocationId = doc.FromLocationId,
                toLocationId = doc.ToLocationId,
                qty = doc.Qty,
                status = doc.Status,
                note = doc.Note,
                createdByUserId = doc.CreatedByUserId,
                createdAt = doc.CreatedAt
            });
        }

        // ====== GET: api/productstock/damage-return/{id}
        [HttpGet("damage-return/{id:int}")]
        public async Task<IActionResult> GetDamageReturn(int id)
        {
            if (!IsAuthorized()) return Unauthorized();

            var q = from d in _context.DamageReturnDocs
                    join p in _context.Products on d.ProductId equals p.Id
                    where d.Id == id
                    select new
                    {
                        d.Id,
                        d.DocNo,
                        d.ProductId,
                        ProductName = p.Name,
                        p.Sku,
                        d.FromLocationId,
                        d.ToLocationId,
                        d.Qty,
                        d.Status,
                        d.Note,
                        d.CreatedByUserId,
                        d.CreatedAt,
                        d.ConfirmedByUserId,
                        d.ConfirmedAt,
                        d.QtyReceive,
                        d.NoteReceive,
                    };

            var doc = await q.FirstOrDefaultAsync();
            if (doc == null) return NotFound();
            return Ok(doc);
        }

        // ====== GET: api/productstock/damage-return/list?fromLocationId=...&top=50
        [HttpGet("damage-return/list")]
        public async Task<IActionResult> ListDamageReturns([FromQuery] int fromLocationId, [FromQuery] int top = 50)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (fromLocationId <= 0) return BadRequest();

            var q = from d in _context.DamageReturnDocs
                    where d.FromLocationId == fromLocationId
                    orderby d.CreatedAt descending
                    select new
                    {
                        d.Id,
                        d.DocNo,
                        d.ProductId,
                        d.Qty,
                        d.QtyReceive,
                        d.Status,
                        d.Note,
                        d.CreatedAt
                    };

            var items = await q.Take(Math.Clamp(top, 1, 200)).ToListAsync();
            return Ok(items);
        }

        [HttpPost("damage-return/confirm")]
        public async Task<IActionResult> DamageReturnConfirm([FromBody] DamageReturnConfirmDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto is null || dto.Id <= 0 || dto.QtyReceive <= 0) return BadRequest();

            await SetUserSessionContextAsync(dto.PerformedByUserId ?? 0);

            var doc = await _context.DamageReturnDocs.FirstOrDefaultAsync(x => x.Id == dto.Id);
            if (doc == null) return NotFound("Document not found.");
            if (string.Equals(doc.Status, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
                return Conflict("Document already CONFIRMED.");

            var toIsWh = await _context.Locations.Where(l => l.Id == doc.ToLocationId)
                .Select(l => l.isWarehouse).FirstOrDefaultAsync();
            if (toIsWh != 1) return BadRequest("Confirm must be done at Warehouse.");

            var outstanding = doc.Qty - doc.QtyReceive;
            if (outstanding <= 0) return Conflict("Nothing left to receive.");
            var qty = Math.Min(dto.QtyReceive, outstanding);

            // สร้าง sub-ref ต่อรอบแบบ deterministic: รับสะสม “ถึง” เท่านี้
            var refType = "DAMAGE_RETURN_DOC";
            var subRef = $"DRCONF-{doc.Id}-UPTO-{doc.QtyReceive + qty}";

            // กันกดซ้ำ (ไอดีเดียวกันถือว่าทำสำเร็จไปแล้ว)
            var dup = await _context.StockTransactions.AnyAsync(st =>
                st.ProductId == doc.ProductId &&
                st.ReferenceType == refType &&
                st.ReferenceId == subRef &&
                st.ReasonCode == "WH_RECEIVE_DAMAGE");
            if (dup) return NoContent();

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                await EnsureStockRowAsync(doc.ProductId, doc.FromLocationId);
                await EnsureStockRowAsync(doc.ProductId, doc.ToLocationId);

                // 0) ต้นทาง (STOREHOUSE): OnHand -qty, Damaged -qty
                var decOn = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE dbo.ProductStocks
                SET QtyOnHand = QtyOnHand - {2}, UpdatedAt = SYSUTCDATETIME()
                WHERE ProductId = {0} AND LocationId = {1} AND QtyOnHand >= {2};",
                doc.ProductId, doc.FromLocationId, qty);
                if (decOn == 0) { await tx.RollbackAsync(); return Conflict("Not enough OnHand at storehouse."); }

                var decDam = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE dbo.ProductStocks
                SET QtyDamaged = QtyDamaged - {2}, UpdatedAt = SYSUTCDATETIME()
                WHERE ProductId = {0} AND LocationId = {1} AND QtyDamaged >= {2};",
                doc.ProductId, doc.FromLocationId, qty);
                if (decDam == 0) { await tx.RollbackAsync(); return Conflict("Not enough Damaged at storehouse."); }

                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.StockTransactions
                (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES ({doc.ProductId}, {doc.FromLocationId}, {doc.ToLocationId}, {qty},
                'DAMAGE_RETURN_OUT', N'{refType}', {subRef}, {dto.PerformedByUserId}, {dto.Note ?? doc.Note});");

                // 1) ปลายทาง (WAREHOUSE): ต้องมี QtyReceive พอ แล้วค่อย +OnHand และ -QtyReceive
                var recv = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.ProductStocks
                SET QtyOnHand = QtyOnHand + {qty},
                QtyReceive = QtyReceive - {qty},
                UpdatedAt  = SYSUTCDATETIME()
                WHERE ProductId = {doc.ProductId} AND LocationId = {doc.ToLocationId}
                AND QtyReceive >= {qty};");
                if (recv == 0) { await tx.RollbackAsync(); return Conflict("Not enough QtyReceive at warehouse."); }

                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.StockTransactions
                (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES ({doc.ProductId}, NULL, {doc.ToLocationId}, {qty},
                'WH_RECEIVE_DAMAGE', N'{refType}', {subRef}, {dto.PerformedByUserId}, {dto.Note ?? doc.Note});");

                // 2) Mark เป็นของเสียที่ Warehouse
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.ProductStocks
                SET QtyDamaged = QtyDamaged + {qty}, UpdatedAt = SYSUTCDATETIME()
                WHERE ProductId = {doc.ProductId} AND LocationId = {doc.ToLocationId};");

                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.StockTransactions
                (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES ({doc.ProductId}, {doc.ToLocationId}, NULL, {qty},
                'WH_DAMAGE_ADD', N'{refType}', {subRef}, {dto.PerformedByUserId}, {dto.Note ?? doc.Note});");

                // 3) อัปเดตเอกสาร
                doc.QtyReceive += qty;
                doc.NoteReceive = dto.Note ?? doc.NoteReceive;
                doc.Status = (doc.QtyReceive == doc.Qty) ? "CONFIRMED" : "OVERDUE";
                if (doc.Status == "CONFIRMED")
                {
                    doc.ConfirmedByUserId = dto.PerformedByUserId;
                    doc.ConfirmedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
                return NoContent();
            }
            catch (SqlException ex)
            {
                await tx.RollbackAsync();
                return StatusCode(409, new { message = "Confirm failed (SqlException)", error = ex.Message });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "Confirm failed (Exception)", error = ex.Message });
            }
        }


        // ====== POST: api/productstock/warehouse-to-damagehouse (Admin) ======
        // ลบที่ Warehouse: Damaged -qty (ต้องพอ) และโอน OnHand จาก Warehouse -> Damagehouse
        // เพิ่มที่ Damagehouse: OnHand +qty, Damaged +qty
        [HttpPost("warehouse-to-damagehouse")]
        public async Task<IActionResult> WarehouseToDamagehouse([FromBody] MoveToDamagehouseDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto is null || dto.ProductId <= 0 || dto.FromWarehouseId <= 0 || dto.ToDamagehouseId <= 0 || dto.Qty <= 0)
                return BadRequest();

            await SetUserSessionContextAsync(dto.PerformedByUserId ?? 0);

            var flagsFrom = await _context.Locations.Where(l => l.Id == dto.FromWarehouseId)
                .Select(l => new { l.isWarehouse, l.isDamagehouse }).FirstOrDefaultAsync();
            var flagsTo = await _context.Locations.Where(l => l.Id == dto.ToDamagehouseId)
                .Select(l => new { l.isWarehouse, l.isDamagehouse }).FirstOrDefaultAsync();

            if (flagsFrom == null || flagsTo == null) return NotFound("Location not found.");
            if (flagsFrom.isWarehouse != 1 || flagsTo.isDamagehouse != 1)
                return BadRequest("Route must be Warehouse -> Damagehouse.");

            await using var tx = await _context.Database.BeginTransactionAsync();
            await EnsureStockRowAsync(dto.ProductId, dto.ToDamagehouseId);

            // 0) ต้องมี Damaged พอที่ Warehouse
            var decDam = await _context.Database.ExecuteSqlRawAsync(@"
            UPDATE dbo.ProductStocks
            SET QtyDamaged = QtyDamaged - {2}, UpdatedAt = SYSUTCDATETIME()
            WHERE ProductId = {0} AND LocationId = {1}
            AND QtyDamaged >= {2};",
            dto.ProductId, dto.FromWarehouseId, dto.Qty);

            if (decDam == 0)
            {
                await tx.RollbackAsync();
                return Conflict("Not enough damaged quantity at warehouse.");
            }

            // 1) โอน OnHand จาก Warehouse -> Damagehouse
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_AdjustOrTransferStock @ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note",
                new SqlParameter("@ProductId", dto.ProductId),
                new SqlParameter("@FromLocationId", dto.FromWarehouseId),
                new SqlParameter("@ToLocationId", dto.ToDamagehouseId),
                new SqlParameter("@Qty", dto.Qty),
                new SqlParameter("@ReasonCode", "WH_TO_DAMAGEHOUSE"),
                new SqlParameter("@ReferenceType", DBNull.Value),
                new SqlParameter("@ReferenceId", DBNull.Value),
                new SqlParameter("@PerformedByUserId", (object?)dto.PerformedByUserId ?? DBNull.Value),
                new SqlParameter("@Note", (object?)dto.Note ?? DBNull.Value)
            );

            // 2) เพิ่ม Damaged ที่ Damagehouse + LOG คนละเหตุผล เพื่อบอก state change คนละอย่าง
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.ProductStocks
            SET QtyDamaged = QtyDamaged + {dto.Qty}, UpdatedAt = SYSUTCDATETIME()
            WHERE ProductId = {dto.ProductId} AND LocationId = {dto.ToDamagehouseId};");

            var refId = MakeIdemRefId("DH_DAMAGE_ADD", dto.ProductId, dto.FromWarehouseId, dto.Qty, dto.PerformedByUserId,"");

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.StockTransactions
            (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
            VALUES
            ({dto.ProductId}, NULL, {dto.ToDamagehouseId}, {dto.Qty},
            'DH_DAMAGE_ADD', 'DAMAGE_WH_TO_DH', {refId}, {dto.PerformedByUserId}, {dto.Note});");

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/cycle-count ======
        [HttpPost("cycle-count")]
        public async Task<IActionResult> CycleCount([FromBody] CycleCountDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto is null || dto.LocationId <= 0 || dto.ProductId <= 0)
                return BadRequest();

            await SetUserSessionContextAsync(dto.PerformedByUserId ?? 0);

            await using var tx = await _context.Database.BeginTransactionAsync();

            var ps = await _context.ProductStocks
                .Where(x => x.ProductId == dto.ProductId && x.LocationId == dto.LocationId)
                .Select(x => new { x.QtyOnHand })
                .FirstOrDefaultAsync();

            if (ps is null) return NotFound("Stock row not found.");
            var onHand = ps.QtyOnHand;
            var counted = Math.Max(0, dto.CountedQty);

            if (counted == onHand)
            {
                await tx.CommitAsync();
                return StatusCode(201, new { message = "Count OK", onHand });
            }

            if (counted > onHand)
            {
                var add = counted - onHand;
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.sp_AdjustOrTransferStock @ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note",
                    new SqlParameter("@ProductId", dto.ProductId),
                    new SqlParameter("@FromLocationId", DBNull.Value),
                    new SqlParameter("@ToLocationId", dto.LocationId),
                    new SqlParameter("@Qty", add),
                    new SqlParameter("@ReasonCode", "CYCLE_COUNT_GAIN"),
                    new SqlParameter("@ReferenceType", (object?)dto.ReferenceType ?? DBNull.Value),
                    new SqlParameter("@ReferenceId", (object?)dto.ReferenceId ?? DBNull.Value),
                    new SqlParameter("@PerformedByUserId", (object?)dto.PerformedByUserId ?? DBNull.Value),
                    new SqlParameter("@Note", (object?)dto.Note ?? DBNull.Value)
                );
                await tx.CommitAsync();
                return StatusCode(201, new { message = "Gain adjusted", onHandBefore = onHand, onHandAfter = counted });
            }

            // shortage
            if (string.IsNullOrWhiteSpace(dto.ReasonCode))
                return BadRequest("reasonCode is required when countedQty < onHand.");

            var shortQty = onHand - counted;

            var headId = await ResolveWarehouseHeadLocationIdAsync(dto.PerformedByUserId ?? 0);
            if (headId <= 0) return NotFound("WarehouseHead not found.");

            await EnsureStockRowAsync(dto.ProductId, headId);

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_AdjustOrTransferStock @ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note",
                new SqlParameter("@ProductId", dto.ProductId),
                new SqlParameter("@FromLocationId", dto.LocationId),
                new SqlParameter("@ToLocationId", headId),
                new SqlParameter("@Qty", shortQty),
                new SqlParameter("@ReasonCode", "CYCLE_COUNT_SHORT"),
                new SqlParameter("@ReferenceType", (object?)dto.ReferenceType ?? DBNull.Value),
                new SqlParameter("@ReferenceId", (object?)dto.ReferenceId ?? DBNull.Value),
                new SqlParameter("@PerformedByUserId", (object?)dto.PerformedByUserId ?? DBNull.Value),
                new SqlParameter("@Note", (object?)dto.Note ?? DBNull.Value)
            );

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.ProductStocks
                   SET QtyDamaged = QtyDamaged + {shortQty}, UpdatedAt = SYSUTCDATETIME()
                 WHERE ProductId = {dto.ProductId} AND LocationId = {headId};");

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.StockTransactions
                    (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES
                    ({dto.ProductId}, NULL, {headId}, {shortQty}, 'DAMAGE_ADD', {dto.ReferenceType}, {dto.ReferenceId}, {dto.PerformedByUserId}, {dto.Note});");

            await tx.CommitAsync();
            return StatusCode(201, new { message = "Short transferred to head as damaged", onHandBefore = onHand, onHandAfter = counted });
        }
    }
}

