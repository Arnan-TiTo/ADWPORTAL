using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using miniApp.API.Data;
using miniApp.API.Dtos;
using miniApp.API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace miniApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public ProductController(AppDbContext context, IWebHostEnvironment env, IConfiguration config)
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
        public async Task<IActionResult> GetAll()
        {
            if (!IsAuthorized()) return Unauthorized();

            var products = await _context.Products
                .Include(p => p.Category)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Sku = p.Sku,
                    Quantity = p.Quantity,
                    Note = p.Note,
                    ImageUrl = p.ImageUrl,
                    UserId = p.UserId,
                    LocationId = p.LocationId,
                    CreatedAt = p.CreatedAt,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Price = p.Price,
                    BrandId = p.BrandId,
                    BrandName = p.Brand != null ? p.Brand.Name : null
                })
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("ByCategory")]
        public async Task<IActionResult> GetByCategory([FromQuery] int categoryId)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Location)
                .Include(p => p.User)
                .Where(p => p.CategoryId == categoryId)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Sku = p.Sku,
                    Description = p.Description,
                    LocationId = p.LocationId,
                    LocationName = p.Location != null ? p.Location.Name : "", 
                    UserId = p.UserId,
                    UserFullname = p.User != null ? p.User.Fullname : "", 
                    Quantity = p.Quantity,
                    Note = p.Note,
                    CreatedAt = p.CreatedAt,
                    ImageUrl = p.ImageUrl,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Price = p.Price,
                    BrandId = p.BrandId,
                    BrandName = p.Brand != null ? p.Brand.Name : null,
                })
                .ToListAsync();

            return Ok(products);
        }


        [HttpGet("ProductSearch")]
        public async Task<IActionResult> SearchProducts([FromQuery] string query, [FromQuery] int? categoryId)
        {
            if (!IsAuthorized()) return Unauthorized();

            if (string.IsNullOrWhiteSpace(query) && categoryId == null)
                return BadRequest("Query or CategoryId is required.");

            var matched = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Location)
                .Include(p => p.User)
                .Include(p => p.Brand)
                .Where(p =>
                    (string.IsNullOrWhiteSpace(query) || p.Name.Contains(query) || (p.Sku != null && p.Sku.Contains(query))) &&
                    (categoryId == null || p.CategoryId == categoryId)
                )
                .ToListAsync();

            var result = matched.Select(p => new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                Description = p.Description,
                LocationId = p.LocationId,
                LocationName = p.Location?.Name ?? "",
                UserId = p.UserId,
                UserFullname = p.User?.Fullname ?? "",
                Quantity = p.Quantity,
                Note = p.Note,
                CreatedAt = p.CreatedAt,
                ImageUrl = p.ImageUrl,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                Price = p.Price,
                BrandId = p.BrandId,
                BrandName = p.Brand != null ? p.Brand.Name : null,
            }).ToList();

            return Ok(result);
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();

            if (!await _context.Locations.AnyAsync(l => l.Id == dto.LocationId))
                return BadRequest("Invalid LocationId");

            string? imagePath = null;
            if (dto.Image != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(dto.Image.FileName);
                var savePath = Path.Combine(_env.WebRootPath, "images/products", fileName);
                using var stream = new FileStream(savePath, FileMode.Create);
                await dto.Image.CopyToAsync(stream);
                imagePath = "/images/products/" + fileName;
            }


            if (dto.CreatedAt == default)
            {
                dto.CreatedAt = DateTime.UtcNow;
            }
    

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Sku = dto.Sku,
                Quantity = dto.Quantity,
                Note = dto.Note,
                UserId = dto.UserId,
                LocationId = dto.LocationId,
                ImageUrl = imagePath,
                CreatedAt = dto.CreatedAt,
                Price = dto.Price,
                BrandId = dto.BrandId
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(product);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ProductUpdateDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == dto.Id);
            if (product == null) return NotFound();

            if (!await _context.Locations.AnyAsync(l => l.Id == dto.LocationId))
                return BadRequest("Invalid LocationId");

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Sku = dto.Sku;
            product.Quantity = dto.Quantity;
            product.Note = dto.Note;
            product.LocationId = dto.LocationId;
            product.Price = dto.Price;
            product.BrandId = dto.BrandId;


            if (dto.Image != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(dto.Image.FileName);
                var savePath = Path.Combine(_env.WebRootPath, "images/products", fileName);
                using var stream = new FileStream(savePath, FileMode.Create);
                await dto.Image.CopyToAsync(stream);
                product.ImageUrl = "/images/products/" + fileName;
            }

            await _context.SaveChangesAsync();

            return Ok(product);
        }


        [HttpPut("updatequantities")]
        public async Task<IActionResult> UpdateQuantities([FromBody] List<UpdateQuantityDto> dtos)
        {
            if (!IsAuthorized()) return Unauthorized();

            if (dtos == null || dtos.Count == 0)
                return BadRequest("No data to update.");

            var productIds = dtos.Select(d => d.Id).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            foreach (var product in products)
            {
                var dto = dtos.First(d => d.Id == product.Id);
                product.Quantity = dto.Quantity;
            }

            await _context.SaveChangesAsync();
            return Ok(new { updated = products.Count });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthorized()) return Unauthorized();

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
