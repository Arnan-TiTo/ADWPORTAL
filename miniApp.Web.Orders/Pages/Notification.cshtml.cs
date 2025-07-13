using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using miniApp.API.Models;
using System.Security.Claims;

namespace miniApp.WebOrders.Pages
{
    public class NotificationModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NotificationModel(IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }

        public void OnGet()
        {
            var USERID = HttpContext.Session.GetInt32("USERID") ?? 0;
            ViewData["USERID"] = USERID;
            ViewData["APIBASEURL"] = _config["APIBASEURL"] ?? "";
            ViewData["AUTHTOKEN"] = _config["AUTHTOKEN"] ?? "";
        }
    }
}
