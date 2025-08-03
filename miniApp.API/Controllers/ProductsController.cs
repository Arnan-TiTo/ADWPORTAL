using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;
using miniApp.API.Data;
using miniApp.API.Dtos;
using miniApp.API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        @page "/products"
@inject HttpClient Http
@inject NavigationManager Navigation
@inject IJSRuntime JSRuntime

<h3> Product List</h3>

@if(products == null)
        {
    < p >< em > Loading...</ em ></ p >
}
else if (!products.Any())
{
    <p>No products found.</p>
}
else
{
    <table class="table table-bordered table-hover">
        <thead class="table-light">
            <tr>
                <th>Image</th>
                <th>Name</th>
                <th>SKU</th>
                <th>Category</th>
                <th>Price</th>
                <th>Qty</th>
                <th style = "width:120px;" > Action </ th >
            </ tr >
        </ thead >
        < tbody >
            @foreach(var p in products)
            {
                <tr>
                    <td>
                        @if(!string.IsNullOrEmpty(p.ImageUrl))
    {
                            < img src = "@($"{ ApiBaseUrl}
        { p.ImageUrl}
        ")" alt = "@p.Name" width = "50" height = "50" />
                        }
                        else
                        {
                            <span>-</span>
                        }
                    </ td >
                    < td > @p.Name </ td >
                    < td > @p.Sku </ td >
                    < td > @p.CategoryName </ td >
                    < td > @p.Price.ToString("C") </ td >
                    < td > @p.Quantity </ td >
                    < td >
                        < button class= "btn btn-sm btn-primary me-2" @onclick = "() => EditProduct(p.Id)" >
                            < i class= "bi bi-pencil-square" ></ i >
                        </ button >
                        < button class= "btn btn-sm btn-danger" @onclick = "() => DeleteProduct(p.Id)" >
                            < i class= "bi bi-trash" ></ i >
                        </ button >
                    </ td >
                </ tr >
            }
        </ tbody >
    </ table >
}

@code {
    private List<ProductDto>? products;
private string ApiBaseUrl = "http://localhost/appapi"; // ค่า base URL

protected override async Task OnInitializedAsync()
{
    // ดึง Token จาก Environment Variable (Registry)
    var token = Environment.GetEnvironmentVariable("AuthToken", EnvironmentVariableTarget.Machine);

    if (string.IsNullOrEmpty(token))
    {
        await JSRuntime.InvokeVoidAsync("alert", "Auth token not found. Please login.");
        Navigation.NavigateTo("/login");
        return;
    }

    Http.BaseAddress = new Uri(ApiBaseUrl);
    Http.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    try
    {
        products = await Http.GetFromJsonAsync<List<ProductDto>>("api/Product");
    }
    catch (Exception ex)
    {
        await JSRuntime.InvokeVoidAsync("alert", $"Failed to load products: {ex.Message}");
    }
}

private void EditProduct(int id)
{
    Navigation.NavigateTo($"/product-edit/{id}");
}

private async Task DeleteProduct(int id)
{
    var confirm = await JSRuntime.InvokeAsync<bool>("confirm", $"Delete product ID {id}?");
    if (confirm)
    {
        var response = await Http.DeleteAsync($"api/Product/{id}");
        if (response.IsSuccessStatusCode)
        {
            products?.Remove(products.First(p => p.Id == id));
            StateHasChanged();
        }
        else
        {
            await JSRuntime.InvokeVoidAsync("alert", "Failed to delete product");
        }
    }
}

public class ProductDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public string? CategoryName { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
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
