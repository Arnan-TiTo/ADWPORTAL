using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Text.Json;

namespace miniApp.Web.Pages.Session
{
    public class SetJwtModel : PageModel
    {
        public class TokenInput
        {
            public string Token { get; set; } = "";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var input = JsonSerializer.Deserialize<TokenInput>(body);

            if (string.IsNullOrWhiteSpace(input?.Token))
            {
                return BadRequest("Token required.");
            }

            HttpContext.Session.SetString("JWT", input.Token);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "QRUser"),
                new Claim("JWT", input.Token)
            };
            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("MyCookieAuth", principal);

            return new JsonResult(new { success = true });
        }
    }
}
