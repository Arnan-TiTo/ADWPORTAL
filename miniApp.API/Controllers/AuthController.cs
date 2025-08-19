using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using miniApp.API.Auth;
using miniApp.API.Data;
using miniApp.API.Dtos;
using miniApp.API.Models;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace miniApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest("Username already exists.");

            var hashed = BCrypt.Net.BCrypt.HashPassword(dto.Password);
           
            var user = new User
            {
                Username = dto.Username,
                PasswordHash = hashed,
                Fullname = dto.Fullname,
                Phone = dto.Phone,
                Email = dto.Email,
                Role = dto.Role?.Trim() ?? "",
                QrLogin = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == login.Username && u.isDelete == 0); 
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash))
                return Unauthorized("Invalid username or password.");

            if (user.isActive != 1)
                return Unauthorized("Account is not active.");

            if (user.Role != login.Role)
                return Unauthorized("Invalid account access app.");

            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                token,
                userId = user.Id,
                fullname = user.Fullname
            });
        }

    }

    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int isActive { get; set; } 

    }
}
