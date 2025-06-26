using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using miniApp.API.Data;
using miniApp.API.Dtos;
using miniApp.API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace miniApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public LocationsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> CreateLocation(
            [FromForm] string Name,
            [FromForm] string Note,
            [FromForm] float Latitude,
            [FromForm] float Longitude,
            [FromForm] List<IFormFile> Image,
            [FromForm] string Usernames)
        {
            Console.WriteLine($"Name: {Name}, Note: {Note}, Latitude: {Latitude}, Longitude: {Longitude}");

            if (Image != null && Image.Count > 0)
            {
                foreach (var file in Image)
                {
                    Console.WriteLine($"Received file: {file.FileName}");
                }
            }

            var username = Usernames;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Unauthorized();

            var location = new Location
            {
                UserId = user.Id,
                Name = Name,
                Note = Note,
                Latitude = Latitude,
                Longitude = Longitude,
                CreatedAt = DateTime.UtcNow
            };

            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Saved Location with ID: {location.Id}");

            if (Image != null && Image.Count > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
                Directory.CreateDirectory(uploadsFolder);

                foreach (var file in Image)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var image = new LocationImage
                    {
                        LocationId = location.Id,
                        ImageUrl = $"/uploads/{fileName}"
                    };

                    _context.LocationImages.Add(image);
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Location saved", location.Id });
        }
    }
}
