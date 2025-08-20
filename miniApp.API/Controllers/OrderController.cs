using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using miniApp.API.Data;
using miniApp.API.Dtos;
using miniApp.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace miniApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public OrderController(AppDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
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

        private static bool IsValidJsonOrNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            try
            {
                using var _ = JsonDocument.Parse(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderViewDto>>> GetAll()
        {
            if (!IsAuthorized()) return Unauthorized();

            var orders = await _context.OrderHd
                .Include(h => h.OrderDts)
                .ToListAsync();

            var result = orders.Select(h => new OrderViewDto
            {
                Id = h.Id,
                OrderNo = h.OrderNo,
                OrderDate = h.OrderDate,
                CustomerName = h.CustomerName,
                CustomerPhone = h.CustomerPhone,
                CustomerEmail = h.CustomerEmail,
                AddressLine = h.AddressLine,
                SubDistrict = h.SubDistrict,
                District = h.District,
                Province = h.Province,
                ZipCode = h.ZipCode,
                Gender = h.Gender,
                BirthDate = h.BirthDate,
                Occupation = h.Occupation,
                Nationality = h.Nationality,
                MayIAsk = h.MayIAsk,
                PaymentMethod = h.PaymentMethod,
                SlipImage = h.SlipImage,
                Social = h.Social,
                Items = h.OrderDts.Select(d => new OrderItemDto
                {
                    LocationId = d.LocationId,
                    ProductId = d.ProductId,
                    ProductName = d.ProductName,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Discount = d.Discount
                }).ToList()
            });

            return Ok(result);
        }

        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<OrderViewDto>>> GetAllHistory()
        {
            if (!IsAuthorized()) return Unauthorized();

            var orders = await _context.OrderHd
                .Include(h => h.OrderDts)
                .ToListAsync();

            var allProductIds = orders.SelectMany(o => o.OrderDts.Select(dt => dt.ProductId)).Distinct().ToList();
            var products = await _context.Products
                .Where(p => allProductIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var result = orders.Select(h => new OrderViewDto
            {
                OrderNo = h.OrderNo,
                OrderDate = h.OrderDate,
                CustomerName = h.CustomerName,
                AddressLine = h.AddressLine,
                SubDistrict = h.SubDistrict,
                District = h.District,
                Province = h.Province,
                ZipCode = h.ZipCode,
                CustomerPhone = h.CustomerPhone,
                CustomerEmail = h.CustomerEmail,
                Social = h.Social,

                Items = h.OrderDts.Select(d =>
                {
                    products.TryGetValue(d.ProductId, out var prod);
                    return new OrderItemDto
                    {
                        LocationId = d.LocationId,
                        ProductId = d.ProductId,
                        ProductName = d.ProductName,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        Discount = d.Discount,
                        ImageUrl = prod?.ImageUrl ?? "",
                    };
                }).ToList()
            });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto is null || dto.Items is null || dto.Items.Count == 0)
                return BadRequest("Body & items are required.");
            if (!IsValidJsonOrNull(dto.Social))
                return BadRequest("Social must be a valid JSON string or null.");

            // เอา USERID จาก session/claims ไปตั้ง session context
            int userId = 0;
            try
            {
                var claim = HttpContext.User?.FindFirst("USERID")?.Value;
                int.TryParse(claim, out userId);
            }
            catch { userId = 0; }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (userId > 0)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC sys.sp_set_session_context @key=N'user_id', @value={0};", userId);
                }

                // เตรียม TVP
                var tvp = new System.Data.DataTable();
                tvp.Columns.Add("ProductId", typeof(int));
                tvp.Columns.Add("ProductName", typeof(string));
                tvp.Columns.Add("UnitPrice", typeof(decimal));
                tvp.Columns.Add("Quantity", typeof(int));
                tvp.Columns.Add("Discount", typeof(decimal));
                tvp.Columns.Add("LocationId", typeof(int));

                foreach (var i in dto.Items)
                {
                    tvp.Rows.Add(i.ProductId, i.ProductName ?? "", i.UnitPrice, i.Quantity, i.Discount, i.LocationId);
                }

                var p = new[]
                {
                    new SqlParameter("@CustomerName",  (object?)dto.CustomerName  ?? DBNull.Value),
                    new SqlParameter("@CustomerPhone", (object?)dto.CustomerPhone ?? DBNull.Value),
                    new SqlParameter("@CustomerEmail", (object?)dto.CustomerEmail ?? DBNull.Value),
                    new SqlParameter("@AddressLine",   (object?)dto.AddressLine   ?? DBNull.Value),
                    new SqlParameter("@SubDistrict",   (object?)dto.SubDistrict   ?? DBNull.Value),
                    new SqlParameter("@District",      (object?)dto.District      ?? DBNull.Value),
                    new SqlParameter("@Province",      (object?)dto.Province      ?? DBNull.Value),
                    new SqlParameter("@ZipCode",       (object?)dto.ZipCode       ?? DBNull.Value),
                    new SqlParameter("@Gender",        (object?)dto.Gender        ?? ""),
                    new SqlParameter("@BirthDate",     (object?)dto.BirthDate     ?? DBNull.Value),
                    new SqlParameter("@Occupation",    (object?)dto.Occupation    ?? ""),
                    new SqlParameter("@Nationality",   (object?)dto.Nationality   ?? ""),
                    new SqlParameter("@MayIAsk",       dto.MayIAsk),
                    new SqlParameter("@PaymentMethod", (object?)dto.PaymentMethod ?? ""),
                    new SqlParameter("@SlipImage",     (object?)dto.SlipImage     ?? DBNull.Value),
                    new SqlParameter("@Social",        (object?)dto.Social        ?? DBNull.Value),

                    new SqlParameter("@Items", tvp)
                    {
                        SqlDbType = System.Data.SqlDbType.Structured,
                        TypeName = "dbo.OrderItemTYPE"
                    },

                    new SqlParameter("@OrderId", System.Data.SqlDbType.Int)
                    { Direction = System.Data.ParameterDirection.Output },

                    new SqlParameter("@OrderNo", System.Data.SqlDbType.NVarChar, 50)
                    { Direction = System.Data.ParameterDirection.Output },
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.sp_Order_Create " +
                    "@CustomerName,@CustomerPhone,@CustomerEmail,@AddressLine,@SubDistrict,@District,@Province,@ZipCode," +
                    "@Gender,@BirthDate,@Occupation,@Nationality,@MayIAsk,@PaymentMethod,@SlipImage,@Social," +
                    "@Items,@OrderId OUT,@OrderNo OUT", p);

                await tx.CommitAsync();

                var newId = (int)p[^2].Value;
                var newNo = (string)p[^1].Value;
                return Ok(new { Id = newId, OrderNo = newNo });
            }
            catch (SqlException ex)
            {
                await tx.RollbackAsync();
                var errors = ex.Errors.Cast<SqlError>().Select(e => new {
                    e.Number,
                    e.State,
                    e.Class,
                    e.LineNumber,
                    e.Procedure,
                    e.Server,
                    Message = e.Message
                }).ToList();

                // แม็พข้อความ stock เป็น 409
                var msg = ex.Message.ToLowerInvariant();
                var status = (msg.Contains("insufficient") || msg.Contains("not found")) ? 409 : 500;

                return StatusCode(status, new
                {
                    message = "Create order failed (SqlException)",
                    error = ex.Message,
                    errors
                });
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync();
                var root = ex.GetBaseException();
                return StatusCode(500, new
                {
                    message = "Create order failed (DbUpdateException)",
                    error = root?.Message ?? ex.Message
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new
                {
                    message = "Create order failed (Exception)",
                    error = ex.Message
                });
            }
        }



        [HttpPut]
        public async Task<IActionResult> Update([FromBody] OrderUpdateDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();

            if (!IsValidJsonOrNull(dto.Social))
                return BadRequest("Social must be a valid JSON string or null.");

            var order = await _context.OrderHd
                .Include(h => h.OrderDts)
                .FirstOrDefaultAsync(h => h.Id == dto.Id);

            if (order == null) return NotFound();

            order.CustomerName = dto.CustomerName;
            order.CustomerPhone = dto.CustomerPhone;
            order.CustomerEmail = dto.CustomerEmail;
            order.AddressLine = dto.AddressLine;
            order.SubDistrict = dto.SubDistrict;
            order.District = dto.District;
            order.Province = dto.Province;
            order.ZipCode = dto.ZipCode;
            order.Gender = dto.Gender ?? "";
            order.BirthDate = dto.BirthDate;
            order.Occupation = dto.Occupation ?? "";
            order.Nationality = dto.Nationality ?? "";
            order.MayIAsk = dto.MayIAsk;
            order.PaymentMethod = dto.PaymentMethod;
            order.SlipImage = dto.SlipImage;
            order.Social = dto.Social;

            _context.OrderDt.RemoveRange(order.OrderDts);

            order.OrderDts = dto.Items.Select(i => new OrderDt
            {   
                LocationId = i.LocationId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Discount = i.Discount
            }).ToList();

            await _context.SaveChangesAsync();

            return Ok(new { order.Id, order.OrderNo });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthorized()) return Unauthorized();

            var order = await _context.OrderHd.Include(h => h.OrderDts).FirstOrDefaultAsync(h => h.Id == id);
            if (order == null) return NotFound();

            _context.OrderDt.RemoveRange(order.OrderDts);
            _context.OrderHd.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderViewDto>> GetById(int id)
        {
            if (!IsAuthorized()) return Unauthorized();

            var order = await _context.OrderHd.Include(h => h.OrderDts).FirstOrDefaultAsync(h => h.Id == id);
            if (order == null) return NotFound();

            var dto = new OrderViewDto
            {
                Id = order.Id,
                OrderNo = order.OrderNo,
                OrderDate = order.OrderDate,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                CustomerEmail = order.CustomerEmail,
                AddressLine = order.AddressLine,
                SubDistrict = order.SubDistrict,
                District = order.District,
                Province = order.Province,
                ZipCode = order.ZipCode,
                Gender = order.Gender,
                BirthDate = order.BirthDate,
                Occupation = order.Occupation,
                Nationality = order.Nationality,
                MayIAsk = order.MayIAsk,
                PaymentMethod = order.PaymentMethod,
                SlipImage = order.SlipImage,
                Social = order.Social,
                Items = order.OrderDts.Select(d => new OrderItemDto
                {   
                    LocationId = d.LocationId,
                    ProductId = d.ProductId,
                    ProductName = d.ProductName,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Discount = d.Discount
                }).ToList()
            };

            return Ok(dto);
        }
    }
}
