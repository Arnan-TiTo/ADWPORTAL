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
            var p = new SqlParameter("@userId", userId);
            await _context.Database.ExecuteSqlRawAsync("EXEC sys.sp_set_session_context @key=N'user_id', @value=@userId", p);
        }


        // ====== GET: api/productstock (paged list with filters, scoped by user) ======
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
                        p.Price,
                        TotalQtyAllLocations = p.Quantity,
                        CategoryName = (string?)null,
                        BrandName = (string?)null
                    };

            var items = await q.OrderBy(x => x.Name).ToListAsync();
            return Ok(items);
        }

        // ====== POST: api/productstock/adjust  (รับเข้า/ตัดออก single-leg) ======
        [HttpPost("adjust")]
        public async Task<IActionResult> Adjust([FromBody] AdjustStockDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand == 0) return BadRequest("ProductId, LocationId, Qty are required.");
            if (string.IsNullOrWhiteSpace(dto.ReasonCode)) dto.ReasonCode = dto.QtyOnHand > 0 ? "PURCHASE" : "ISSUE";

            await using var tx = await _context.Database.BeginTransactionAsync();

            var p = new[]
            {
                new SqlParameter("@ProductId", dto.ProductId),
                new SqlParameter("@FromLocationId", dto.QtyOnHand < 0 ? dto.LocationId : (object)DBNull.Value),
                new SqlParameter("@ToLocationId",   dto.QtyOnHand > 0 ? dto.LocationId : (object)DBNull.Value),
                new SqlParameter("@Qty", Math.Abs(dto.QtyOnHand)),
                new SqlParameter("@ReasonCode", dto.ReasonCode),
                new SqlParameter("@ReferenceType", (object?)dto.ReferenceType ?? DBNull.Value),
                new SqlParameter("@ReferenceId",   (object?)dto.ReferenceId   ?? DBNull.Value),
                new SqlParameter("@PerformedByUserId", (object?)dto.PerformedByUserId ?? DBNull.Value),
                new SqlParameter("@Note", (object?)dto.Note ?? DBNull.Value),
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_AdjustOrTransferStock @ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note",
                p
            );

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/transfer  ======
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
                "EXEC dbo.sp_AdjustOrTransferStock @ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note",
                p
            );

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/reserve  ======
        [HttpPost("reserve")]
        public async Task<IActionResult> Reserve([FromBody] ReserveDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0) return BadRequest("ProductId, LocationId, positive Qty are required.");

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

            //location ลง log ด้วย (FromLocationId = LocationId)
            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO dbo.StockTransactions
                    (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES
                    ({0}, {1}, NULL, {2}, 'RESERVE', {3}, {4}, {5}, {6});",
                dto.ProductId, dto.LocationId, dto.QtyOnHand,
                (object?)dto.ReferenceType ?? DBNull.Value,
                (object?)dto.ReferenceId ?? DBNull.Value,
                (object?)dto.PerformedByUserId ?? DBNull.Value,
                (object?)dto.Note ?? DBNull.Value
            );

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/release-reserve  ======
        [HttpPost("release-reserve")]
        public async Task<IActionResult> ReleaseReserve([FromBody] ReserveDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0) return BadRequest("ProductId, LocationId, positive Qty are required.");

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

            //location ลง log ด้วย
            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO dbo.StockTransactions
                    (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES
                    ({0}, {1}, NULL, {2}, 'RESERVE_CANCEL', {3}, {4}, {5}, {6});",
                dto.ProductId, dto.LocationId, dto.QtyOnHand,
                (object?)dto.ReferenceType ?? DBNull.Value,
                (object?)dto.ReferenceId ?? DBNull.Value,
                (object?)dto.PerformedByUserId ?? DBNull.Value,
                (object?)dto.Note ?? DBNull.Value
            );

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/ship-from-reserved  (optional) ======
        [HttpPost("ship-from-reserved")]
        public async Task<IActionResult> ShipFromReserved([FromBody] ReserveDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0) return BadRequest("ProductId, LocationId, positive Qty are required.");

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
                "EXEC dbo.sp_AdjustOrTransferStock @ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note",
                p
            );

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/damage-add  ======
        [HttpPost("damage-add")]
        public async Task<IActionResult> DamageAdd([FromBody] DamageDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0) return BadRequest("ProductId, LocationId, positive Qty are required.");

            await using var tx = await _context.Database.BeginTransactionAsync();

            var affected = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE dbo.ProductStocks
                   SET QtyDamaged = QtyDamaged + {2}, UpdatedAt = SYSUTCDATETIME()
                 WHERE ProductId = {0} AND LocationId = {1}
                   AND (QtyOnHand - QtyReserved - QtyDamaged) >= {2};",
                dto.ProductId, dto.LocationId, dto.QtyOnHand);

            if (affected == 0)
            {
                await tx.RollbackAsync();
                return Conflict("Insufficient available quantity to mark as damaged.");
            }

            // location ลง log
            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO dbo.StockTransactions
                    (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES
                    ({0}, {1}, NULL, {2}, 'DAMAGE_ADD', {3}, {4}, {5}, {6});",
                dto.ProductId, dto.LocationId, dto.QtyOnHand,
                (object?)dto.ReferenceType ?? DBNull.Value,
                (object?)dto.ReferenceId ?? DBNull.Value,
                (object?)dto.PerformedByUserId ?? DBNull.Value,
                (object?)dto.Note ?? DBNull.Value
            );

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== POST: api/productstock/damage-repair  ======
        [HttpPost("damage-repair")]
        public async Task<IActionResult> DamageRepair([FromBody] DamageDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0) return BadRequest("ProductId, LocationId, positive Qty are required.");

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

            // ผูก location ลง log
            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO dbo.StockTransactions
                    (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES
                    ({0}, {1}, NULL, {2}, 'DAMAGE_REPAIR', {3}, {4}, {5}, {6});",
                dto.ProductId, dto.LocationId, dto.QtyOnHand,
                (object?)dto.ReferenceType ?? DBNull.Value,
                (object?)dto.ReferenceId ?? DBNull.Value,
                (object?)dto.PerformedByUserId ?? DBNull.Value,
                (object?)dto.Note ?? DBNull.Value
            );

            await tx.CommitAsync();
            return NoContent();
        }

        // ====== GET: api/productstock/audit?locationId=&productId=&top=50  ======
        [HttpGet("audit")]
        public async Task<IActionResult> Audit([FromQuery] int locationId, [FromQuery] int productId, [FromQuery] int top = 50)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (locationId <= 0 || productId <= 0) return BadRequest("locationId and productId are required.");

            // รวม transaction ที่เกี่ยวกับ location นั้น: จาก From หรือ To
            var q = from st in _context.StockTransactions
                    join u in _context.Users on st.PerformedByUserId equals u.Id into uj
                    from u in uj.DefaultIfEmpty()
                    where st.ProductId == productId
                          && (st.FromLocationId == locationId || st.ToLocationId == locationId
                              || (st.FromLocationId == null && st.ToLocationId == null))
                    orderby st.CreatedAt descending
                    select new
                    {
                        st.CreatedAt,
                        Action = st.ReasonCode,
                        Qty = Math.Abs(st.QtyChange),
                        st.Note,
                        ByUser = (u.Fullname ?? u.Username)
                    };

            var items = await q.Take(Math.Clamp(top, 1, 500)).ToListAsync();

            return Ok(items.Select(x => new
            {
                x.CreatedAt,
                x.Action,
                x.Qty,
                x.Note,
                x.ByUser
            }));
        }

        // ====== POST: api/productstock/add-row  (สร้างแถว stock และรับเข้าตั้งต้นได้) ======
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
                // 1) กันซ้ำ
                var exists = await _context.ProductStocks
                    .AnyAsync(x => x.ProductId == dto.ProductId && x.LocationId == dto.LocationId);
                if (exists) return Conflict("Row already exists for this product & location.");

                // 2) เพิ่มแถวเริ่มต้น
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.ProductStocks
              (ProductId, LocationId, QtyOnHand, QtyReserved, QtyDamaged,
               MinLevel, MaxLevel, ReorderPoint, Cost, UpdatedAt)
            VALUES
              ({dto.ProductId}, {dto.LocationId}, 0, 0, 0,
               NULL, NULL, NULL, 0, SYSUTCDATETIME());");

                // 3) รับเข้าเริ่มต้น (บันทึก log ผ่าน sp)
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
                        "EXEC dbo.sp_AdjustOrTransferStock " +
                        "@ProductId, @FromLocationId, @ToLocationId, @Qty, " +
                        "@ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note", p);
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



        // ====== DELETE: api/productstock/{locationId}/{productId}  (ลบสินค้าได้ เมื่อยอดคงเหลือ=0) ======
        [HttpDelete("{locationId:int}/{productId:int}")]
        public async Task<IActionResult> DeleteRow(int locationId, int productId)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (locationId <= 0 || productId <= 0) return BadRequest();

            try
            {
                // อ่านเฉพาะคอลัมน์ที่ต้องใช้ (กัน null) 
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

                // ลบด้วยคำสั่ง SQL ตรง ๆ (ไม่มี OUTPUT จึงไม่ชน trigger)
                var affected = await _context.Database.ExecuteSqlRawAsync(
                    @"DELETE FROM dbo.ProductStocks 
              WHERE LocationId = {0} AND ProductId = {1};",
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



        [HttpGet("dropdown")]
        public async Task<IActionResult> ProductDropdown([FromQuery] string? q = null, [FromQuery] int top = 50)
        {
            if (!IsAuthorized()) return Unauthorized();

            var query = _context.Products.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p => (p.Name ?? "").Contains(q) || (p.Sku ?? "").Contains(q));
            }

            var items = await query
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name, p.Sku })
                .Take(Math.Clamp(top, 1, 200))
                .ToListAsync();

            return Ok(items);
        }

        // GET: api/products/dropdown-not-in-location?locationId=1&q=…
        [HttpGet("dropdown-not-in-location")]
        public async Task<IActionResult> ProductDropdownNotInLocation(
            [FromQuery] int locationId,
            [FromQuery] string? q = null,
            [FromQuery] int top = 50)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (locationId <= 0) return BadRequest("locationId is required.");

            var baseQuery = _context.Products.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                baseQuery = baseQuery.Where(p => (p.Name ?? "").Contains(q) || (p.Sku ?? "").Contains(q));
            }

            var items = await (from p in baseQuery
                               where !_context.ProductStocks.Any(ps => ps.ProductId == p.Id && ps.LocationId == locationId)
                               orderby p.Name
                               select new { p.Id, p.Name, p.Sku })
                              .Take(Math.Clamp(top, 1, 200))
                              .ToListAsync();

            return Ok(items);
        }

        // POST: api/productstock/damage-writeoff
        [HttpPost("damage-writeoff")]
        public async Task<IActionResult> DamageWriteOff([FromBody] DamageDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null) return BadRequest();
            if (dto.ProductId <= 0 || dto.LocationId <= 0 || dto.QtyOnHand <= 0)
                return BadRequest("ProductId, LocationId, positive Qty are required.");

            await using var tx = await _context.Database.BeginTransactionAsync();

            // 1) ต้องมีกองเสียพอให้ตัดทิ้ง
            var affected = await _context.Database.ExecuteSqlRawAsync(@"
        UPDATE dbo.ProductStocks
           SET QtyDamaged = QtyDamaged - {2}, UpdatedAt = SYSUTCDATETIME()
         WHERE ProductId = {0} AND LocationId = {1}
           AND QtyDamaged >= {2};",
                dto.ProductId, dto.LocationId, dto.QtyOnHand);

            if (affected == 0)
            {
                await tx.RollbackAsync();
                return Conflict("Not enough damaged quantity to write-off.");
            }

            // 2) ตัดของออกจากสต็อกจริง (ออกจากสาขานี้)
            var p = new[]
            {
                new SqlParameter("@ProductId", dto.ProductId),
                new SqlParameter("@FromLocationId", dto.LocationId),
                new SqlParameter("@ToLocationId", DBNull.Value),
                new SqlParameter("@Qty", dto.QtyOnHand),
                new SqlParameter("@ReasonCode", "DAMAGE_WRITE_OFF"),
                new SqlParameter("@ReferenceType", (object?)dto.ReferenceType ?? DBNull.Value),
                new SqlParameter("@ReferenceId",   (object?)dto.ReferenceId   ?? DBNull.Value),
                new SqlParameter("@PerformedByUserId", (object?)dto.PerformedByUserId ?? DBNull.Value),
                new SqlParameter("@Note", (object?)dto.Note ?? DBNull.Value),
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_AdjustOrTransferStock @ProductId, @FromLocationId, @ToLocationId, @Qty, " +
                "@ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note", p);

            await tx.CommitAsync();
            return NoContent();
        }
    }
}
