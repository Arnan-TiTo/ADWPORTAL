using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.WebOrders.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace miniApp.WebOrders.Pages
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

        public void OnGet()
        {
            Register = new UserRequest();
            ErrorMessage = null;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

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
            [Required(ErrorMessage = "Full name is required.")]
            public string Fullname { get; set; } = "";

            [Required(ErrorMessage = "Username is required.")]
            public string Username { get; set; } = "";

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            public string Email { get; set; } = "";

            [Required(ErrorMessage = "Phone is required.")]
            [RegularExpression(@"^0[689]\d{8}$", ErrorMessage = "Phone must be 10 digits starting with 06, 08, or 09.")]
            public string Phone { get; set; } = "";

            [Required(ErrorMessage = "Password is required.")]
            public string Password { get; set; } = "";
        }
    }
}
