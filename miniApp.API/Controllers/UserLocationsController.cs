using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using miniApp.API.Data;
using miniApp.API.Dtos;
using miniApp.API.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace miniApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserLocationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public UserLocationsController(AppDbContext context, IConfiguration config)
        {
            _context = context;
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

        // GET: api/userlocations?userId=&locationId=
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? userId, [FromQuery] int? locationId)
        {
            if (!IsAuthorized()) return Unauthorized();

            var q = _context.UserLocations.AsQueryable();
            if (userId.HasValue) q = q.Where(x => x.UserId == userId.Value);
            if (locationId.HasValue) q = q.Where(x => x.LocationId == locationId.Value);

            var items = await q
                .OrderBy(x => x.UserId).ThenBy(x => x.LocationId)
                .Select(x => new UserLocationDto
                {
                    UserId = x.UserId,
                    LocationId = x.LocationId,
                    User = null,       // เพื่อเลี่ยง expose ข้อมูลอ่อนไหว
                    Location = null
                })
                .ToListAsync();

            return Ok(items);
        }

        // GET: api/userlocations/user/123
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            if (!IsAuthorized()) return Unauthorized();

            var exists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!exists) return NotFound("User not found.");

            var items = await _context.UserLocations
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.LocationId)
                .Select(x => new UserLocationDto { UserId = x.UserId, LocationId = x.LocationId })
                .ToListAsync();

            return Ok(items);
        }

        // GET: api/userlocations/location/10
        [HttpGet("location/{locationId:int}")]
        public async Task<IActionResult> GetByLocation(int locationId)
        {
            if (!IsAuthorized()) return Unauthorized();

            var exists = await _context.Locations.AnyAsync(l => l.Id == locationId);
            if (!exists) return NotFound("Location not found.");

            var items = await _context.UserLocations
                .Where(x => x.LocationId == locationId)
                .OrderBy(x => x.UserId)
                .Select(x => new UserLocationDto { UserId = x.UserId, LocationId = x.LocationId })
                .ToListAsync();

            return Ok(items);
        }

        // POST: api/userlocations
        // body: { "userId": 1, "locationId": 10 }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserLocationDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (dto == null || dto.UserId <= 0 || dto.LocationId <= 0)
                return BadRequest("UserId and LocationId are required.");

            // validate foreign keys
            var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists) return NotFound("User not found.");
            var locExists = await _context.Locations.AnyAsync(l => l.Id == dto.LocationId);
            if (!locExists) return NotFound("Location not found.");

            var exists = await _context.UserLocations
                .AnyAsync(x => x.UserId == dto.UserId && x.LocationId == dto.LocationId);
            if (exists) return Conflict("Mapping already exists.");

            _context.UserLocations.Add(new UserLocation
            {
                UserId = dto.UserId,
                LocationId = dto.LocationId
            });
            await _context.SaveChangesAsync();

            return Ok(new UserLocationDto { UserId = dto.UserId, LocationId = dto.LocationId });
        }

        // DELETE: api/userlocations/user/1/location/10
        [HttpDelete("user/{userId:int}/location/{locationId:int}")]
        public async Task<IActionResult> Delete(int userId, int locationId)
        {
            if (!IsAuthorized()) return Unauthorized();

            var entity = await _context.UserLocations
                .FirstOrDefaultAsync(x => x.UserId == userId && x.LocationId == locationId);
            if (entity == null) return NotFound();

            _context.UserLocations.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/userlocations/user/1
        // body: { "locationIds": [2,3,5] }
        // จะ "แทนที่" รายการ location ทั้งหมดของ user ให้ตรงตามลิสต์ที่ส่งมา (atomic)
        [HttpPut("user/{userId:int}")]
        public async Task<IActionResult> ReplaceUserLocations(int userId, [FromBody] UpdateUserLocationsDto body)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (body == null) return BadRequest("Body is required.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            var newSet = (body.LocationIds ?? Array.Empty<int>()).Distinct().ToHashSet();

            // ตรวจสอบว่า location ที่ระบุมีอยู่จริงทั้งหมด
            var validLocIds = await _context.Locations
                .Where(l => newSet.Contains(l.Id))
                .Select(l => l.Id)
                .ToListAsync();
            if (validLocIds.Count != newSet.Count)
                return BadRequest("Some locationIds are invalid.");

            using var tx = await _context.Database.BeginTransactionAsync();

            var current = await _context.UserLocations
                .Where(x => x.UserId == userId)
                .Select(x => x.LocationId)
                .ToListAsync();

            var currentSet = current.ToHashSet();

            // คำนวณ diff
            var toAdd = newSet.Except(currentSet).ToList();
            var toRemove = currentSet.Except(newSet).ToList();

            if (toRemove.Count > 0)
            {
                var removeRows = await _context.UserLocations
                    .Where(x => x.UserId == userId && toRemove.Contains(x.LocationId))
                    .ToListAsync();
                _context.UserLocations.RemoveRange(removeRows);
            }

            if (toAdd.Count > 0)
            {
                var addRows = toAdd.Select(locId => new UserLocation
                {
                    UserId = userId,
                    LocationId = locId
                });
                await _context.UserLocations.AddRangeAsync(addRows);
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            // คืนผลลัพธ์ปัจจุบัน
            var result = await _context.UserLocations
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.LocationId)
                .Select(x => new UserLocationDto { UserId = x.UserId, LocationId = x.LocationId })
                .ToListAsync();

            return Ok(result);
        }
    }
}
