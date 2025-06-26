using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.Web.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace miniApp.Web.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly IConfiguration _config;
        public string ApiBaseUrl => _config["ApiBaseUrl"] ?? "http://localhost:5252";
        public LoginModel(AuthService authService, IConfiguration config)
        {
            _authService = authService;
            _config = config;
        }

        [BindProperty]
        public LoginRequest Login { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var token = await _authService.LoginAsync(Login);
            if (string.IsNullOrEmpty(token))
            {
                ErrorMessage = "Invalid credentials.";
                return Page();
            }

            HttpContext.Session.SetString("JWT", token);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Login.Username),
                new Claim("JWT", token)
            };

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("MyCookieAuth", principal);

            return RedirectToPage("/Index");
        }

        public class LoginRequest
        {
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
        }
    }
}
