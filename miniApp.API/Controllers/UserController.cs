using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using miniApp.API.Data;
using miniApp.API.Dtos;
using miniApp.API.Models;
using miniApp.API.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using static miniApp.API.Controllers.UsersController;

namespace miniApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _email;
        private readonly QrService _qr;
        private readonly IConfiguration _config;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AppDbContext context, EmailService email, QrService qr, IConfiguration config, ILogger<UsersController> logger)
        {
            _context = context;
            _email = email;
            _qr = qr;
            _config = config;
            _logger =  logger;
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

            var users = await _context.Users
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Fullname = u.Fullname,
                    Email = u.Email ?? "",
                    Phone = u.Phone ?? "",
                    Role = u.Role.ToString(),
                    QrLogin = u.QrLogin,
                    IsApproveQr = u.isApproveQr
                }).ToListAsync();

            return Ok(users);
        }

        // GET api/users/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserResponseDto>> GetById(int id)
        {
            if (!IsAuthorized()) return Unauthorized();

            var u = await _context.Users.FindAsync(id);
            if (u == null) return NotFound();
            return Ok(new UserResponseDto
            {
                Id = u.Id,
                Username = u.Username,
                Fullname = u.Fullname,
                Email = u.Email ?? "",
                Phone = u.Phone ?? "",
                Role = u.Role.ToString(), 
                QrLogin = u.QrLogin,
                IsApproveQr = u.isApproveQr
            });
        }

        // POST api/users
        [HttpPost]
        public async Task<ActionResult<object>> Create([FromBody] UserDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("Username & Password are required.");

            var exists = await _context.Users.AnyAsync(x => x.Username == dto.Username);
            if (exists) return Conflict("Username already exists.");

            RoleType role = RoleType.Staff;
            if (Enum.TryParse(dto.Role, true, out RoleType parsedRole))
            {
                role = parsedRole;
            }
            else
            {
                role = RoleType.Staff;
            }

            var user = new User
            {
                Username = dto.Username.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Fullname = dto.Fullname?.Trim() ?? "",
                Email = dto.Email?.Trim() ?? "",
                Phone = dto.Phone?.Trim() ?? "",
                Role = role,
                QrLogin = dto.QrLogin ?? string.Empty,
                isApproveQr = dto.IsApproveQr
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { Id = user.Id });
        }

        // PUT api/users/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();

            var u = await _context.Users.FindAsync(id);
            if (u == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Password))
                u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            u.Fullname = dto.Fullname?.Trim() ?? u.Fullname;
            u.Email = dto.Email?.Trim() ?? "";
            u.Phone = dto.Phone?.Trim() ?? "";
            
            RoleType role = RoleType.Staff;
            if (Enum.TryParse(dto.Role, true, out RoleType parsedRole))
            {
                role = parsedRole;
            }
            else
            {
                role = RoleType.Staff;
            }
            u.Role = role;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/users/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthorized()) return Unauthorized();

            var u = await _context.Users.FindAsync(id);
            if (u == null) return NotFound();
            _context.Users.Remove(u);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT api/users/{id}/approve-qr
        [HttpPut("{id:int}/approve-qr")]
        public async Task<IActionResult> ApproveQr(int id)
        {
            if (!IsAuthorized()) return Unauthorized();

            var u = await _context.Users.FindAsync(id);
            if (u == null) return NotFound();
            if (string.IsNullOrWhiteSpace(u.QrLogin)) return BadRequest("User has no QR.");

            u.isApproveQr = 1; // approve
            _context.Notifications.Add(new Notification
            {
                UserId = u.Id,
                Title = "Login by QR",
                Message = "Login ด้วย QR ได้รับการอนุมัติแล้ว",
                Type = "Info",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT api/users/{id}/revoke-qr
        [HttpPut("{id:int}/revoke-qr")]
        public async Task<IActionResult> RevokeQr(int id)
        {
            if (!IsAuthorized()) return Unauthorized();

            var u = await _context.Users.FindAsync(id);
            if (u == null) return NotFound();
            u.isApproveQr = 0;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST api/users/{id}/generate-qr
        [HttpPost("{id:int}/generate-qr")]
        public async Task<ActionResult<object>> GenerateQrAndEmail(int id)
        {
            if (!IsAuthorized()) return Unauthorized();

            var u = await _context.Users.FindAsync(id);
            if (u == null) return NotFound();
            if (string.IsNullOrWhiteSpace(u.Email)) return BadRequest("User has no email.");

            var qr = Guid.NewGuid().ToString("N");
            u.QrLogin = qr;
            u.isApproveQr = 0;
            await _context.SaveChangesAsync();

            // Build QR PNG
            var png = _qr.MakePng(qr, pixelsPerModule: 10);
            var subject = "Your new QR for login";
            var body = $@"<p>สวัสดี {System.Net.WebUtility.HtmlEncode(u.Fullname ?? u.Username)},</p>
                          <p>นี่คือ QR ใหม่สำหรับใช้เข้าสู่ระบบ (ต้องได้อนุมัติจากผู้ดูแลระบบก่อนใช้งาน)</p>
                          <p>QR string: <b>{qr}</b></p>";

            try
            {
                await _email.SendAsync(u.Email!, subject, body, new[] { new EmailAttachment("qr.png", "image/png", png) });
            }
            catch (Exception ex)
            {

               _logger.LogError(ex, "Send email failed for user {UserId}", u.Id);

                return StatusCode(500, "Email sending failed");
            }


            return Ok(new { Qr = qr });
        }
    }
}