using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;
using miniApp.Web.Services;
using System.Threading.Tasks;

namespace miniApp.Web.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly AuthService _authService;

        public RegisterModel(AuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public UserRequest Register { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var (success, error) = await _authService.RegisterAsync(Register);
            if (!success)
            {
                ErrorMessage = $"Registration failed: {error}";
                return Page();
            }

            return RedirectToPage("/login");
        }

        public class UserRequest
        {
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
            public string Fullname { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";
        }
    }
}
