using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace miniApp.Web.Pages
{
    public class LocationModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public string Usernames { get; set; } = "";
        public LocationModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            ViewData["ApiBaseUrl"] = _configuration["ApiBaseUrl"];
            ViewData["ApiAuthToken"] = _configuration["AuthToken"]; 
            Usernames = User.Identity?.Name ?? "";
        }

        public string Username => User.Identity?.Name ?? "Guest";
        public string RoleDescription => User.IsInRole("admin") ? "Administrator" : "Staff";
        public string CurrentDate => DateTime.Now.ToString("yyyy-MM-dd");
        public string CurrentTime => DateTime.Now.ToString("HH:mm:ss");
    }
}
