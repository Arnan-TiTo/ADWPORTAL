using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace miniApp.WebOrders.Pages
{
    public class LogoutModel : PageModel
    {
        public async Task<IActionResult> OnPostAsync()
        {
            HttpContext.Session.Remove("JWT");
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToPage("/Login");
        }

        // Optional: support GET redirect too
        public async Task<IActionResult> OnGetAsync()
        {
            return await OnPostAsync();
        }
    }
}
