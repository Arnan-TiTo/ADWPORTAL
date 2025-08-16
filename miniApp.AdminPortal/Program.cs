using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using miniApp.AdminPortal.Components;
using miniApp.AdminPortal.Models;
using miniApp.AdminPortal.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// === Fixed API token ===
var fixedToken = Environment.GetEnvironmentVariable("AuthToken", EnvironmentVariableTarget.Machine);
builder.Services.AddSingleton(new AuthTokenProvider(fixedToken ?? ""));

// === Config API base ===
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));

// === Cookie Auth + Authorization ===
builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    o.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(o =>
{
    o.Cookie.Name = "Miniapp.AdminPortal.AuthCookie";
    o.LoginPath = "/login";
    o.LogoutPath = "/logout";
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
});
builder.Services.AddAuthorization();

// === Session ===
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = "Miniapp.AdminPortal.Auth";
    o.IdleTimeout = TimeSpan.FromHours(8);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<AuthMessageHandler>();
builder.Services.AddHttpClient("ApiClient", (sp, http) =>
{
    var cfg = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
    http.BaseAddress = new Uri(cfg.ApiBaseUrl);
})
.AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(o => o.DetailedErrors = true);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery(); 

//session logout
app.MapPost("/auth/set-session", async (HttpContext ctx, SetSessionDto dto) =>
{
    if (dto is null || string.IsNullOrWhiteSpace(dto.Token))
        return Results.BadRequest("Invalid token");

    ctx.Session.SetString("JWT", dto.Token);
    ctx.Session.SetInt32("USERID", dto.UserId);
    ctx.Session.SetString("FULLNAME", dto.Fullname ?? "");

    var claims = new List<System.Security.Claims.Claim>
    {
        new(System.Security.Claims.ClaimTypes.Name, dto.Username ?? ""),
        new("USERID", dto.UserId.ToString()),
        new("JWT", dto.Token),
        new("FULLNAME", dto.Fullname ?? "")
    };
    var id = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var cp = new System.Security.Claims.ClaimsPrincipal(id);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, cp);

    await ctx.Session.CommitAsync();

    return Results.Ok(new { success = true });
})
.AllowAnonymous()
.DisableAntiforgery();

app.MapPost("/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    ctx.Session.Clear();
    await ctx.Session.CommitAsync();

    var opts = new CookieOptions
    {
        Path = "/",
        Secure = ctx.Request.IsHttps,
        HttpOnly = true,
        SameSite = SameSiteMode.Lax
    };

    ctx.Response.Cookies.Delete("Miniapp.AdminPortal.Auth", opts);
    ctx.Response.Cookies.Delete("Miniapp.AdminPortal.AuthCookie", opts);

    return Results.Ok(new { success = true });
})
.AllowAnonymous()
.DisableAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();

// DTO set-session
public record SetSessionDto(string Token, int UserId, string? Fullname, string? Username);

public class AuthTokenProvider { public string Token { get; } public AuthTokenProvider(string t) => Token = t; }
