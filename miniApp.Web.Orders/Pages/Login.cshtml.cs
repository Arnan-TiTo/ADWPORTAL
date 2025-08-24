using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.WebOrders.Services;
using System.Linq;
using System.Security.Claims;
using miniApp.WebOrders.Dtos;

namespace miniApp.WebOrders.Pages
{
    [IgnoreAntiforgeryToken]
    public class LoginModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly IConfiguration _config;

        public string ApiBaseUrl => _config["ApiBaseUrl"] ?? "";

        public LoginModel(AuthService authService, IConfiguration config)
        {
            _authService = authService;
            _config = config;
        }

        [BindProperty]
        public LoginRequest Login { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostSetSessionAsync([FromBody] SetSessionDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Token))
                return BadRequest("Invalid token data");

            HttpContext.Session.SetString("JWT", dto.Token);
            HttpContext.Session.SetInt32("USERID", dto.UserId);
            HttpContext.Session.SetString("FULLNAME", dto.Fullname);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, dto.Username),
                new Claim("USERID", dto.UserId.ToString()),
                new Claim("JWT", dto.Token),
                new Claim("FULLNAME", dto.Fullname)
            };

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                "MyCookieAuth",
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,      
                    AllowRefresh = true        
                });

            return new JsonResult(new { success = true });
        }

        public class SetSessionDto
        {
            public string Token { get; set; } = "";
            public int UserId { get; set; }
            public string Fullname { get; set; } = "";
            public string Username { get; set; } = "";
        }

    }
}
