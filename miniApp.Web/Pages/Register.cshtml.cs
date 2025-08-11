using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace miniApp.Web.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public RegisterModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            ViewData["ApiBaseUrl"] = _configuration["ApiBaseUrl"];
        }
        public string? ErrorMessage { get; set; }
    }
}
