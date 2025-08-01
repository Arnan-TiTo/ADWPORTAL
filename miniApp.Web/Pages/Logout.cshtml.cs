using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace miniApp.Web.Pages
{
    public class LogoutModel : PageModel
    {
        public async Task<IActionResult> OnPostAsync()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync("MyCookieAuth", new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow
            });

            return RedirectToPage("/Login");
        }

        // Optional: support GET redirect too
        public async Task<IActionResult> OnGetAsync()
        {
            return await OnPostAsync();
        }
    }
}
