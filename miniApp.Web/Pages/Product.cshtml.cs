using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace miniApp.Web.Pages
{
    public class ProductModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ProductModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            ViewData["APIBASEURL"] = _configuration["ApiBaseUrl"] ?? "";
            ViewData["AUTHTOKEN"] = _configuration["AUTHTOKEN"] ??
             Environment.GetEnvironmentVariable("AuthToken") ?? "";
        }
    }
}
