using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using miniApp.API.Data;
using miniApp.API.Models;
using miniApp.API.Dtos;
using System;
using System.Linq;
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

        // ===== Simple service-to-service token like your sample =====
        private bool IsAuthorized()
        {
            var expectedToken = _config["AUTH_TOKEN"];
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                return false;

            var token = authHeader.Substring("Bearer ".Length).Trim();
            return token == expectedToken;
        }

        // ===== Helper: (optional) set session context for RLS if you enabled it =====
        private async Task SetUserSessionContextAsync(int userId)
        {
            // If you didn't enable RLS, this is harmless/no-op.
            var p = new SqlParameter("@userId", userId);
            await _context.Database.ExecuteSqlRawAsync("EXEC sys.sp_set_session_context @key=N'user_id', @value=@userId", p);
        }

        // ===== Helper: user can see this location? (when not using RLS) =====
        private async Task<bool> UserHasAccessAsync(int userId, int locationId)
        {
            return await _context.UserLocations
                .AnyAsync(ul => ul.UserId == userId && ul.LocationId == locationId);
        }

        // ====== GET: api/productstock (paged list with filters, scoped by user) ======
        // Query params: userId (required), locationId?, productId?, search?, page=1, pageSize=50
        [HttpGet]
        public async Task<IActionResult> GetStocks([FromQuery] int userId, [FromQuery] int? locationId,
                                                   [FromQuery] int? productId, [FromQuery] string? search,
                                                   [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (userId <= 0) return BadRequest("userId is required.");

            await SetUserSessionContextAsync(userId); // for RLS (optional)

            var q = from ps in _context.ProductStocks
                    join p in _context.Products on ps.ProductId equals p.Id
                    join l in _context.Locations on ps.LocationId equals l.Id
                    // If you do NOT use RLS, uncomment the WHERE EXISTS to enforce app-side filtering:
                    // where _context.UserLocations.Any(ul => ul.UserId == userId && ul.LocationId == ps.LocationId)
                    select new
                    {
                        ps.ProductId,
                        p.Name,
                        p.Sku,
                        ps.LocationId,
                        LocationName = l.Name,
                        ps.QtyOnHand,
                        ps.QtyReserved,
                        ps.QtyDamaged,
                        ps.QtyAvailable,
                        p.Price,
                        TotalQtyAllLocations = p.Quantity
                    };

            if (locationId.HasValue) q = q.Where(x => x.LocationId == locationId.Value);
            if (productId.HasValue) q = q.Where(x => x.ProductId == productId.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(x => (x.Name ?? "").Contains(s) || (x.Sku ?? "").Contains(s) || (x.LocationName ?? "").Contains(s));
            }

            var total = await q.CountAsync();
            var items = await q
                .OrderBy(x => x.Name).ThenBy(x => x.LocationName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        // ====== GET: api/productstock/product/{productId}?userId=...  breakdown by location ======
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProduct(int productId, [FromQuery] int userId)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (userId <= 0) return BadRequest("userId is required.");

            await SetUserSessionContextAsync(userId);

            var q = from ps in _context.ProductStocks
                    join p in _context.Products on ps.ProductId equals p.Id
                    join l in _context.Locations on ps.LocationId equals l.Id
                    // where _context.UserLocations.Any(ul => ul.UserId == userId && ul.LocationId == ps.LocationId)
                    where ps.ProductId == productId
                    select new
                    {
                        ps.ProductId,
                        p.Name,
                        p.Sku,
                        ps.LocationId,
                        LocationName = l.Name,
                        ps.QtyOnHand,
                        ps.QtyReserved,
                        ps.QtyDamaged,
                        ps.QtyAvailable,
                        p.Price,
                        TotalQtyAllLocations = p.Quantity
                    };

            var items = await q.OrderBy(x => x.LocationName).ToListAsync();
            return Ok(items);
        }

        // ====== GET: api/productstock/location/{locationId}?userId=...  all products at one location ======
        [HttpGet("location/{locationId}")]
        public async Task<IActionResult> GetByLocation(int locationId, [FromQuery] int userId)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (userId <= 0) return BadRequest("userId is required.");

            await SetUserSessionContextAsync(userId);

            // If not using RLS, enforce:
            // if (!await UserHasAccessAsync(userId, locationId)) return Forbid();

            var q = from ps in _context.ProductStocks
                    join p in _context.Products on ps.ProductId equals p.Id
                    join l in _context.Locations on ps.LocationId equals l.Id
                    where ps.LocationId == locationId
                    select new
                    {
                        ps.ProductId,
                        p.Name,
                        p.Sku,
                        ps.LocationId,
                        LocationName = l.Name,
                        ps.QtyOnHand,
                        ps.QtyReserved,
                        ps.QtyDamaged,
                        ps.QtyAvailable,
                        p.Price,
                        TotalQtyAllLocations = p.Quantity
                    };

            var items = await q.OrderBy(x => x.Name).ToListAsync();
            return Ok(items);
        }

        // ====== POST: api/productstock/adjust  (รับเข้า/ตัดออก single-leg) ======
        // body: { productId, locationId, qty, reasonCode, referenceType?, referenceId?, performedByUserId?, note? }
        [HttpPost("adjust")]
        public async Task<IActionResult> Adjust([FromBody] AdjustStockDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand == 0) return BadRequest("ProductId, LocationId, Qty are required.");
            if (string.IsNullOrWhiteSpace(dto.ReasonCode)) dto.ReasonCode = dto.QtyOnHand > 0 ? "PURCHASE" : "ISSUE";

            // If not using RLS:
            // if (!await UserHasAccessAsync(dto.PerformedByUserId ?? 0, dto.LocationId)) return Forbid();

            await using var tx = await _context.Database.BeginTransactionAsync();

            // call stored procedure
            var p = new[]
            {
                new SqlParameter("@ProductId", dto.ProductId),
                new SqlParameter("@FromLocationId", dto.QtyOnHand < 0 ? dto.LocationId : (object)DBNull.Value),
                new SqlParameter("@ToLocationId",   dto.QtyOnHand > 0 ? dto.LocationId : (object)DBNull.Value),
                new SqlParameter("@Qty", Math.Abs(dto.QtyOnHand)), // sp expects positive for transfer; for single-leg we pass abs and From/To determines +/- inside sp
                new SqlParameter("@ReasonCode", dto.ReasonCode),
                new SqlParameter("@ReferenceType", (object?)dto.ReferenceType ?? DBNull.Value),
                new SqlParameter("@ReferenceId",   (object?)dto.ReferenceId   ?? DBNull.Value),
                new SqlParameter("@PerformedByUserId", (object?)dto.PerformedByUserId ?? DBNull.Value),
                new SqlParameter("@Note", (object?)dto.Note ?? DBNull.Value),
            };

            // For single-leg we pass either From or To; sp handles +/- accordingly.
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC miniapp.dbo.sp_AdjustOrTransferStock @ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note",
                p
            );

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/transfer  (โอนย้ายระหว่างสาขา) ======
        // body: { productId, fromLocationId, toLocationId, qty, referenceType?, referenceId?, performedByUserId?, note? }
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
                "EXEC miniapp.dbo.sp_AdjustOrTransferStock @ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note",
                p
            );

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/reserve  (จองของ) ======
        // body: { productId, locationId, qty, referenceType?, referenceId?, performedByUserId?, note? }
        [HttpPost("reserve")]
        public async Task<IActionResult> Reserve([FromBody] ReserveDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0) return BadRequest("ProductId, LocationId, positive Qty are required.");

            await using var tx = await _context.Database.BeginTransactionAsync();

            // optimistic guard: only increase reserved if available >= qty
            var affected = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE miniapp.dbo.ProductStocks
                   SET QtyReserved = QtyReserved + {2}, UpdatedAt = SYSUTCDATETIME()
                 WHERE ProductId = {0} AND LocationId = {1}
                   AND (QtyOnHand - QtyReserved - QtyDamaged) >= {2};",
                dto.ProductId, dto.LocationId, dto.QtyOnHand);

            if (affected == 0)
            {
                await tx.RollbackAsync();
                return Conflict("Insufficient available quantity to reserve.");
            }

            // audit log (optional): you can log RESERVE in StockTransactions if desired
            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO miniapp.dbo.StockTransactions
                    (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES
                    ({0}, NULL, NULL, {1}, 'RESERVE', {2}, {3}, {4}, {5});",
                dto.ProductId, dto.QtyOnHand,
                (object?)dto.ReferenceType ?? DBNull.Value,
                (object?)dto.ReferenceId ?? DBNull.Value,
                (object?)dto.PerformedByUserId ?? DBNull.Value,
                (object?)dto.Note ?? DBNull.Value
            );

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/release-reserve  (ปล่อยจอง/ยกเลิกจอง) ======
        // body: { productId, locationId, qty, ... }
        [HttpPost("release-reserve")]
        public async Task<IActionResult> ReleaseReserve([FromBody] ReserveDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0) return BadRequest("ProductId, LocationId, positive Qty are required.");

            await using var tx = await _context.Database.BeginTransactionAsync();

            var affected = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE miniapp.dbo.ProductStocks
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
                INSERT INTO miniapp.dbo.StockTransactions
                    (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES
                    ({0}, NULL, NULL, {1}, 'RESERVE_CANCEL', {2}, {3}, {4}, {5});",
                dto.ProductId, dto.QtyOnHand,
                (object?)dto.ReferenceType ?? DBNull.Value,
                (object?)dto.ReferenceId ?? DBNull.Value,
                (object?)dto.PerformedByUserId ?? DBNull.Value,
                (object?)dto.Note ?? DBNull.Value
            );

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/ship-from-reserved  (ตัดส่งของจากยอดจอง) ======
        // body: { productId, locationId, qty, referenceType?, referenceId?, performedByUserId?, note? }
        [HttpPost("ship-from-reserved")]
        public async Task<IActionResult> ShipFromReserved([FromBody] ReserveDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0) return BadRequest("ProductId, LocationId, positive Qty are required.");

            await using var tx = await _context.Database.BeginTransactionAsync();

            // 1) decrease reserved first
            var affected = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE miniapp.dbo.ProductStocks
                   SET QtyReserved = QtyReserved - {2}, UpdatedAt = SYSUTCDATETIME()
                 WHERE ProductId = {0} AND LocationId = {1}
                   AND QtyReserved >= {2};",
                dto.ProductId, dto.LocationId, dto.QtyOnHand);

            if (affected == 0)
            {
                await tx.RollbackAsync();
                return Conflict("Not enough reserved quantity to ship.");
            }

            // 2) cut OnHand via stored procedure (single-leg outbound)
            var p = new[]
            {
                new SqlParameter("@ProductId", dto.ProductId),
                new SqlParameter("@FromLocationId", dto.LocationId),
                new SqlParameter("@ToLocationId", DBNull.Value),
                new SqlParameter("@Qty", dto.QtyOnHand), // outbound
                new SqlParameter("@ReasonCode", "SALE"),
                new SqlParameter("@ReferenceType", (object?)dto.ReferenceType ?? DBNull.Value),
                new SqlParameter("@ReferenceId", (object?)dto.ReferenceId ?? DBNull.Value),
                new SqlParameter("@PerformedByUserId", (object?)dto.PerformedByUserId ?? DBNull.Value),
                new SqlParameter("@Note", (object?)dto.Note ?? DBNull.Value),
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC miniapp.dbo.sp_AdjustOrTransferStock @ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note",
                p
            );

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/damage-add  (พบของเสีย: เพิ่ม Damaged, OnHand ไม่เปลี่ยน) ======
        // body: { productId, locationId, qty, referenceType?, referenceId?, performedByUserId?, note? }
        [HttpPost("damage-add")]
        public async Task<IActionResult> DamageAdd([FromBody] DamageDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0) return BadRequest("ProductId, LocationId, positive Qty are required.");

            await using var tx = await _context.Database.BeginTransactionAsync();

            // Only increase damaged if available >= qty (so available won't go negative)
            var affected = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE miniapp.dbo.ProductStocks
                   SET QtyDamaged = QtyDamaged + {2}, UpdatedAt = SYSUTCDATETIME()
                 WHERE ProductId = {0} AND LocationId = {1}
                   AND (QtyOnHand - QtyReserved - QtyDamaged) >= {2};",
                dto.ProductId, dto.LocationId, dto.QtyOnHand);

            if (affected == 0)
            {
                await tx.RollbackAsync();
                return Conflict("Insufficient available quantity to mark as damaged.");
            }

            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO miniapp.dbo.StockTransactions
                    (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES
                    ({0}, NULL, NULL, {1}, 'DAMAGE_ADD', {2}, {3}, {4}, {5});",
                dto.ProductId, dto.QtyOnHand,
                (object?)dto.ReferenceType ?? DBNull.Value,
                (object?)dto.ReferenceId ?? DBNull.Value,
                (object?)dto.PerformedByUserId ?? DBNull.Value,
                (object?)dto.Note ?? DBNull.Value
            );

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/damage-repair  (ซ่อมของเสีย: ลด Damaged, OnHand ไม่เปลี่ยน) ======
        [HttpPost("damage-repair")]
        public async Task<IActionResult> DamageRepair([FromBody] DamageDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0) return BadRequest("ProductId, LocationId, positive Qty are required.");

            await using var tx = await _context.Database.BeginTransactionAsync();

            var affected = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE miniapp.dbo.ProductStocks
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
                INSERT INTO miniapp.dbo.StockTransactions
                    (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES
                    ({0}, NULL, NULL, {1}, 'DAMAGE_REPAIR', {2}, {3}, {4}, {5});",
                dto.ProductId, dto.QtyOnHand,
                (object?)dto.ReferenceType ?? DBNull.Value,
                (object?)dto.ReferenceId ?? DBNull.Value,
                (object?)dto.PerformedByUserId ?? DBNull.Value,
                (object?)dto.Note ?? DBNull.Value
            );

            await tx.CommitAsync();
            return NoContent();
        }
    }


}
