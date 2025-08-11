using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using miniApp.API.Data;
using miniApp.API.Dtos;
using miniApp.API.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace miniApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductCategoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public ProductCategoryController(AppDbContext context, IConfiguration config, IWebHostEnvironment env)
        {
            _context = context;
            _config = config;
            _env = env;
        }

        // --- ช่วยหาโฟลเดอร์รูปจริง ---
        private string GetImagesPhysicalRoot()
        {
            var configuredRoot = _config["ImageRootPath"]; // เช่น C:\inetpub\wwwroot\images
            if (!string.IsNullOrWhiteSpace(configuredRoot))
                return configuredRoot;

            var webRoot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
            return Path.Combine(webRoot, "images");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _context.ProductCategories
                .Select(c => new ProductCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ImageUrl = c.ImageUrl
                }).ToListAsync();

            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Category name is required.");

            // dto.ImageUrl ควรเป็น "/images/categories/xxx.jpg" หรือเป็น URL เต็มจากฝั่งอัปโหลด
            var category = new ProductCategory
            {
                Name = dto.Name,
                ImageUrl = dto.ImageUrl
            };

            _context.ProductCategories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new ProductCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                ImageUrl = category.ImageUrl
            });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateProductCategoryDto dto)
        {
            var category = await _context.ProductCategories.FindAsync(dto.Id);
            if (category == null)
                return NotFound();

            category.Name = dto.Name;
            category.ImageUrl = dto.ImageUrl;
            await _context.SaveChangesAsync();

            return Ok(new ProductCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                ImageUrl = category.ImageUrl
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null)
                return NotFound();

            _context.ProductCategories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ====== อัปโหลดรูป (ตอนยังไม่รู้ id) ======
        // ส่งไฟล์ขึ้นมาก่อน -> API คืน url เช่น /images/categories/<guid>.jpg
        // แล้วฝั่ง UI ค่อยเอา url นี้ไปใส่ใน Create/Update JSON
        [HttpPost("UploadImage")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var imagesRoot = GetImagesPhysicalRoot();        // .../images
            var dir = Path.Combine(imagesRoot, "categories"); // .../images/categories
            Directory.CreateDirectory(dir);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(dir, fileName);

            using (var fs = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(fs);

            var url = $"/images/categories/{fileName}";
            return Ok(new { imageUrl = url });
        }

        // ====== อัปโหลดรูปพร้อมระบุ category id (เช่น กรณีแก้ไขแล้วอยากอัปใหม่ทันที) ======
        [HttpPost("UploadImage/{id:int}")]
        public async Task<IActionResult> UploadImageForCategory(int id, [FromForm] IFormFile file)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null) return NotFound();

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var imagesRoot = GetImagesPhysicalRoot();
            var dir = Path.Combine(imagesRoot, "categories");
            Directory.CreateDirectory(dir);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(dir, fileName);

            using (var fs = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(fs);

            category.ImageUrl = $"/images/categories/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new ProductCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                ImageUrl = category.ImageUrl
            });
        }
    }
}
