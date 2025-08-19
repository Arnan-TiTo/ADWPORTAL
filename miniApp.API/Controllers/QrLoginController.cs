using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using miniApp.API.Auth;
using miniApp.API.Data;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace miniApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class QrLoginController : ControllerBase
    {


        private readonly IMemoryCache _cache;
        private readonly JwtService _jwtService;
        private readonly AppDbContext _db;

        public QrLoginController(IMemoryCache cache, JwtService jwtService, AppDbContext db)
        {
            _cache = cache;
            _jwtService = jwtService;
            _db = db;
        }

        [HttpGet("generate")]
        public IActionResult GenerateToken()
        {
            var token = Guid.NewGuid().ToString();
            _cache.Set(token, "", TimeSpan.FromMinutes(2));
            return Ok(new { qrToken = token });
        }

        [HttpPost("loginByQr")]
        public async Task<IActionResult> LoginByQr([FromBody] QrLoginDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Qr))
                return BadRequest("QR content is required.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.QrLogin == dto.Qr && u.isDelete == 0);
            if (user == null)
                return Unauthorized("QR is not found.");

            if (user.isApproveQr != 1)
                return Unauthorized("QR is not approved.");

            if (user.isActive != 1)
                return Unauthorized("Account is not active.");

            if (user.Role != dto.Role)
                return Unauthorized("Invalid account access app.");

            var jwt = _jwtService.GenerateToken(user);
            return Ok(new
            {
                token = jwt,
                userId = user.Id,
                fullname = user.Fullname,
                username = user.Username
            });
        }

        public class QrLoginDto
        {
            public string Qr { get; set; } = "";
            public string Role { get; set; } = "";

        }

        [HttpPost("scan")]
        public IActionResult Scan(string token)
        {
            if (_cache.TryGetValue(token, out QrLoginInfo info))
            {
                _cache.Remove(token);

                var user = _db.Users.FirstOrDefault(u => u.Username == info.Username);
                if (user == null || !BCrypt.Net.BCrypt.Verify(info.Password, user.PasswordHash))
                    return Unauthorized();

                var jwt = _jwtService.GenerateToken(user);
                return Ok(new { token = jwt });
            }

            return Unauthorized();
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmQrToken([FromBody] QrConfirmDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized();

            if (!_cache.TryGetValue(dto.QrToken, out _))
                return NotFound("QR token not found or expired");

            var info = new QrLoginInfo { Username = dto.Username, Password = dto.Password };
            _cache.Set(dto.QrToken, info, TimeSpan.FromMinutes(2));

            Console.WriteLine($"[QRLogin] User found: {user.Username}, returning JWT");
            return Ok(new { message = "QR token confirmed" });
        }

       public class QrConfirmDto
        {
            public string QrToken { get; set; } = "";
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
        }

        public class QrLoginInfo
        {
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
        }
    }
}