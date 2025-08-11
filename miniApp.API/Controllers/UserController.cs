using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using miniApp.API.Data;
using miniApp.API.Dtos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace miniApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }
        
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAll()
        {
            var users = await _context.Users
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Fullname = u.Fullname,
                    Email = u.Email ?? "",
                    Phone = u.Phone ?? "",
                    Role = u.Role.ToString()
                })
                .ToListAsync();

            return Ok(users);
        }


        [HttpGet("profile")]
        public async Task<ActionResult<UserResponseDto>> GetUserProfile([FromQuery] string username)
        {
            if (string.IsNullOrEmpty(username))
                return BadRequest("Username is required");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound("User not found");

            var userDto = new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Fullname = user.Fullname,
                Email = user.Email ?? "",
                Phone = user.Phone ?? "",
                Role = user.Role.ToString()
            };

            return Ok(userDto);
        }

        [HttpGet("profilebyid")]
        public async Task<ActionResult<UserResponseDto>> GetUserProfile([FromQuery] int userid)
        {
            if (string.IsNullOrEmpty(userid.ToString()))
                return BadRequest("UserId is required");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userid);
            if (user == null)
                return NotFound("User not found");

            var userDto = new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Fullname = user.Fullname,
                Email = user.Email ?? "",
                Phone = user.Phone ?? "",
                Role = user.Role.ToString()
            };

            return Ok(userDto);
        }
    }
}
