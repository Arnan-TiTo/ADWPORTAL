using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.Web.Services;
using System.Linq;
using System.Security.Claims;
using miniApp.Web.Dtos;

namespace miniApp.Web.Pages
{
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

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            var result = await _authService.LoginAsync(Login);
            if (result == null || string.IsNullOrEmpty(result.token))
            {
                ErrorMessage = "Invalid credentials.";
                return Page();
            }

            HttpContext.Session.SetString("JWT", result.token);
            HttpContext.Session.SetInt32("USERID", result.userid);
            HttpContext.Session.SetString("FULLNAME", result.fullname);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Login.Username),
                new Claim("USERID", result.userid.ToString()),
                new Claim("JWT", result.token),
                new Claim("FULLNAME",result.fullname)
            };

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("MyCookieAuth", principal);

            var safeUrl = string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl)
                ? Url.Page("/Index")
                : returnUrl;

            return Redirect(safeUrl!);
        }
    }
}
