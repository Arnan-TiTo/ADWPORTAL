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
                 .Where(u => u.isDelete == 0)
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Fullname = u.Fullname,
                    Email = u.Email ?? "",
                    Phone = u.Phone ?? "",
                    Role = u.Role.ToString(),
                    QrLogin = u.QrLogin,
                    isApproveQr = u.isApproveQr,
                    isActive = u.isActive
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
                isApproveQr = u.isApproveQr,
                isActive = u.isActive
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

            var user = new User
            {
                Username = dto.Username.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Fullname = dto.Fullname?.Trim() ?? "",
                Email = dto.Email?.Trim() ?? "",
                Phone = dto.Phone?.Trim() ?? "",
                Role = dto.Role?.Trim() ?? "",
                QrLogin = dto.QrLogin ?? string.Empty,
                isApproveQr = dto.isApproveQr,
                isActive = dto.isActive
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { Id = user.Id });
        }

        private static string CanonicalRole(string role)
        {
            var r = (role ?? "").Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant();
            return r switch
            {
                "admin" => "Admin",
                "frontline" => "FrontLine",
                "salereport" => "SaleReport",
                _ => role?.Trim() ?? ""
            };
        }


        // PUT api/users/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserDto dto)
        {
            if (!IsAuthorized()) return Unauthorized();

            var u = await _context.Users.FindAsync(id);
            if (u == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                var newUsername = dto.Username.Trim();

                if (!string.Equals(newUsername, u.Username, StringComparison.OrdinalIgnoreCase))
                {
                    var exists = await _context.Users
                        .AnyAsync(x => x.Id != id && x.Username.ToLower() == newUsername.ToLower());
             
                    if (exists)
                        return Conflict("Username already exists.");

                    u.Username = newUsername;
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.Password))
                u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            if (!string.IsNullOrWhiteSpace(dto.Username))
                u.Username = dto.Username.Trim();

            if (dto.Fullname != null)
                u.Fullname = dto.Fullname.Trim();

            if (dto.Phone != null)
                u.Phone = dto.Phone.Trim();

            if (dto.Email != null)
                u.Email = dto.Email.Trim();

            if (dto.Role != null)
                u.Role = CanonicalRole(dto.Role);

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

            u.isDelete = 1;
            u.isActive = 0;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT api/users/{id}/activate
        [HttpPut("{id:int}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            if (!IsAuthorized()) return Unauthorized();

            var u = await _context.Users.FindAsync(id);
            if (u == null) return NotFound();

            u.isActive = 1;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT api/users/{id}/deactivate
        [HttpPut("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            if (!IsAuthorized()) return Unauthorized();

            var u = await _context.Users.FindAsync(id);
            if (u == null) return NotFound();

            u.isActive = 0;
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