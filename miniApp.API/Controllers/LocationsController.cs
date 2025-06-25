using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using miniApp.API.Data;
using miniApp.API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace miniApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LocationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public LocationController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadLocation([FromForm] LocationUploadRequest request)
        {
            var location = new Location
            {
                Name = request.Name,
                Latitude = (float)request.Latitude,
                Longitude = (float)request.Longitude,
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            if (request.Images != null)
            {
                foreach (var img in request.Images)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(img.FileName)}";
                    var savePath = Path.Combine(_env.WebRootPath, "uploads", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
                    using var stream = System.IO.File.Create(savePath);
                    await img.CopyToAsync(stream);

                    _context.LocationImages.Add(new LocationImage
                    {
                        LocationId = location.Id,
                        ImageUrl = $"/uploads/{fileName}"
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Ok(location);
        }
    }

    public class LocationUploadRequest
    {
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int UserId { get; set; }
        public List<IFormFile>? Images { get; set; }
    }
}