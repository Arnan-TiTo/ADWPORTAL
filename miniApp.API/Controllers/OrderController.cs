using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using miniApp.API.Data;
using miniApp.API.Dtos;
using miniApp.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
                Items = h.OrderDts.Select(d => new OrderItemDto
                {
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

                Items = h.OrderDts.Select(d =>
                {
                    products.TryGetValue(d.ProductId, out var prod);
                    return new OrderItemDto
                    {
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

            var order = new OrderHd
            {
                OrderNo = "OD" + DateTime.Now.Ticks.ToString()[^6..],
                OrderDate = DateTime.UtcNow,
                CustomerName = dto.CustomerName,
                CustomerPhone = dto.CustomerPhone,
                CustomerEmail = dto.CustomerEmail,
                AddressLine = dto.AddressLine,
                SubDistrict = dto.SubDistrict,
                District = dto.District,
                Province = dto.Province,
                ZipCode = dto.ZipCode,
                Gender = dto.Gender ?? "",
                BirthDate = dto.BirthDate,
                Occupation = dto.Occupation ?? "",
                Nationality = dto.Nationality ?? "",
                MayIAsk = dto.MayIAsk,
                PaymentMethod = dto.PaymentMethod,
                SlipImage = dto.SlipImage,
                OrderDts = dto.Items.Select(i => new OrderDt
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Discount = i.Discount
                }).ToList()
            };

            _context.OrderHd.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { order.Id, order.OrderNo });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] OrderUpdateDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();

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

            _context.OrderDt.RemoveRange(order.OrderDts);

            order.OrderDts = dto.Items.Select(i => new OrderDt
            {
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
                Items = order.OrderDts.Select(d => new OrderItemDto
                {
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
