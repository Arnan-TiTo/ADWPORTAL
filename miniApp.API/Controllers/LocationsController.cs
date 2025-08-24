using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    public class LocationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public LocationsController(AppDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            _config = config;
        }

        private bool IsAuthorized()
        {
            var expectedToken = _config["AUTH_TOKEN"];
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ")) return false;
            var token = authHeader.Substring("Bearer ".Length).Trim();
            return token == expectedToken;
        }

        private string GetImagesPhysicalRoot()
        {
            var configuredRoot = _config["ImageRootPath"];
            if (!string.IsNullOrWhiteSpace(configuredRoot)) return configuredRoot;

            var webRoot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
            return Path.Combine(webRoot, "images");
        }


        [HttpPost]
        public async Task<IActionResult> CreateLocation(
            [FromForm] string Name,
            [FromForm] string? Note,
            [FromForm] float Latitude,
            [FromForm] float Longitude,
            [FromForm] string? PlaceName,
            [FromForm] string? Building,
            [FromForm] string? Address,
            [FromForm] string? District,
            [FromForm] string? Province,
            [FromForm] string? Postcode,
            [FromForm] string? ContractPerson,
            [FromForm] string? ContractPhone,
            [FromForm] int? isWarehouse,
            [FromForm] int? isStorehouse,

            [FromForm] List<IFormFile>? Image,
            [FromForm] string Usernames)
        {
            if (!IsAuthorized()) return Unauthorized();

            if (string.IsNullOrWhiteSpace(Usernames)) return BadRequest("Username is required.");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == Usernames);
            if (user == null) return BadRequest("User not found.");

            var location = new Location
            {
                UserId = user.Id,
                Name = Name,
                Note = Note,
                Latitude = Latitude,
                Longitude = Longitude,
                PlaceName = PlaceName,
                Building = Building,
                Address = Address,
                District = District,
                Province = Province,
                Postcode = Postcode,
                ContractPerson = ContractPerson,
                ContractPhone = ContractPhone,
                isWarehouse = isWarehouse,
                isStorehouse = isStorehouse,

                CreatedAt = DateTime.UtcNow
            };

            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            if (Image is { Count: > 0 })
            {
                var imagesRoot = GetImagesPhysicalRoot();
                var dir = Path.Combine(imagesRoot, "locations");
                Directory.CreateDirectory(dir);

                foreach (var file in Image)
                {
                    var ext = Path.GetExtension(file.FileName);
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var fullPath = Path.Combine(dir, fileName);
                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    _context.LocationImages.Add(new LocationImage
                    {
                        LocationId = location.Id,
                        ImageUrl = $"/images/locations/{fileName}"
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Location saved", location.Id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateLocation(
            int id,
            [FromForm] string Name,
            [FromForm] string? Note,
            [FromForm] float Latitude,
            [FromForm] float Longitude,
            [FromForm] string? PlaceName,
            [FromForm] string? Building,
            [FromForm] string? Address,
            [FromForm] string? District,
            [FromForm] string? Province,
            [FromForm] string? Postcode,
            [FromForm] string? ContractPerson,
            [FromForm] string? ContractPhone,
            [FromForm] int? isWarehouse,
            [FromForm] int? isStorehouse,

            [FromForm] List<IFormFile>? Image,
            [FromForm] string Usernames)
        {
            if (!IsAuthorized()) return Unauthorized();

            if (string.IsNullOrWhiteSpace(Usernames)) return BadRequest("Username is required.");
            var newOwner = await _context.Users.FirstOrDefaultAsync(u => u.Username == Usernames);
            if (newOwner == null) return BadRequest("Assigned user not found.");

            var location = await _context.Locations.Include(l => l.Images).FirstOrDefaultAsync(l => l.Id == id);
            if (location == null) return NotFound();

            location.Name = Name;
            location.Note = Note;
            location.Latitude = Latitude;
            location.Longitude = Longitude;
            location.UserId = newOwner.Id;
            location.PlaceName = PlaceName;
            location.Building = Building;
            location.Address = Address;
            location.District = District;
            location.Province = Province;
            location.Postcode = Postcode;
            location.ContractPerson = ContractPerson;
            location.ContractPhone = ContractPhone;
            location.isWarehouse = isWarehouse;
            location.isStorehouse = isStorehouse;

            if (Image is { Count: > 0 })
            {
                var imagesRoot = GetImagesPhysicalRoot();
                var dir = Path.Combine(imagesRoot, "locations");
                Directory.CreateDirectory(dir);

                foreach (var file in Image)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var fullPath = Path.Combine(dir, fileName);
                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    _context.LocationImages.Add(new LocationImage
                    {
                        LocationId = location.Id,
                        ImageUrl = $"/images/locations/{fileName}"
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Location updated" });
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            if (!IsAuthorized()) return Unauthorized();

            var locations = await _context.Locations
                .Include(l => l.User)
                .Include(l => l.Images)
                .Select(l => new LocationResponseDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    Note = l.Note,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    CreatedAt = l.CreatedAt,
                    UserId = l.UserId,
                    Username = l.User != null ? l.User.Username : "",
                    Fullname = l.User != null ? l.User.Fullname : "",
                    PlaceName = l.PlaceName,
                    Building = l.Building,
                    Address = l.Address,
                    District = l.District,
                    Province = l.Province,
                    Postcode = l.Postcode,
                    ContractPerson = l.ContractPerson,
                    ContractPhone = l.ContractPhone,

                    Images = l.Images.Select(img => new LocationImageDto
                    {
                        Id = img.Id,
                        ImageUrl = img.ImageUrl
                    }).ToList()
                })
                .ToListAsync();

            return Ok(locations);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!IsAuthorized()) return Unauthorized();

            var l = await _context.Locations
                .Include(x => x.User)
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (l == null) return NotFound();

            var dto = new LocationResponseDto
            {
                Id = l.Id,
                Name = l.Name,
                Note = l.Note,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                CreatedAt = l.CreatedAt,
                UserId = l.UserId,
                Username = l.User?.Username ?? "",
                Fullname = l.User?.Fullname ?? "",
                PlaceName = l.PlaceName,
                Building = l.Building,
                Address = l.Address,
                District = l.District,
                Province = l.Province,
                Postcode = l.Postcode,
                ContractPerson = l.ContractPerson,
                ContractPhone = l.ContractPhone,

                Images = l.Images.Select(img => new LocationImageDto
                {
                    Id = img.Id,
                    ImageUrl = img.ImageUrl
                }).ToList()
            };
            return Ok(dto);
        }


        [HttpGet("dropdown")]
        public async Task<IActionResult> GetLocationDropdown()
        {
            if (!IsAuthorized()) return Unauthorized();

            var locations = await _context.Locations
                .Select(l => new { l.Id, l.Name })
                .ToListAsync();
            return Ok(locations);
        }
    }
}
