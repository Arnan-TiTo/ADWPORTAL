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
    public class ProductBrandController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductBrandController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var brands = await _context.ProductBrands
                .Select(b => new ProductBrandDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description
                }).ToListAsync();

            return Ok(brands);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductBrandDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Brand name is required.");

            var brand = new ProductBrand
            {
                Name = dto.Name,
                Description = dto.Description
            };

            _context.ProductBrands.Add(brand);
            await _context.SaveChangesAsync();

            return Ok(new ProductBrandDto
            {
                Id = brand.Id,
                Name = brand.Name,
                Description = brand.Description
            });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateProductBrandDto dto)
        {
            var brand = await _context.ProductBrands.FindAsync(dto.Id);
            if (brand == null) return NotFound();

            brand.Name = dto.Name;
            brand.Description = dto.Description;

            await _context.SaveChangesAsync();

            return Ok(new ProductBrandDto
            {
                Id = brand.Id,
                Name = brand.Name,
                Description = brand.Description
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _context.ProductBrands.FindAsync(id);
            if (brand == null) return NotFound();

            _context.ProductBrands.Remove(brand);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
