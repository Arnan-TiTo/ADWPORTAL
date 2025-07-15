using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using miniApp.API.Data;
using miniApp.API.Dtos;
using miniApp.API.Models;
using System.Linq;
using System.Threading.Tasks;

namespace miniApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductCategoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductCategoryController(AppDbContext context)
        {
            _context = context;
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null)
                return NotFound();

            _context.ProductCategories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
