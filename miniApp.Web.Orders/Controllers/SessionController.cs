using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using miniApp.WebOrders.Models;

namespace miniApp.WebOrders.Middleware
{
    [Route("Session")]
    public class SessionController : Controller
    {
        [HttpPost("SetJwt")]
        public async Task<IActionResult> SetJwt([FromBody] TokenDto dto)
        {
            Console.WriteLine("📥 SetJwt received. Token = " + dto.Token);

            HttpContext.Session.SetString("JWT", dto.Token);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(dto.Token);
            var username = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "Unknown";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim("JWT", dto.Token)
            };

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("MyCookieAuth", principal);

            Console.WriteLine("✅ Signed in with MyCookieAuth");

            return Ok();
        }
    }
}
