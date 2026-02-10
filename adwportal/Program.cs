using adwportal.Components;
using adwportal.Models;
using adwportal.Security;
using adwportal.Services;
using adwportal.States;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

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
    o.Cookie.Name = "adwPortal.AuthCookie";
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
    o.Cookie.Name = "adwPortal.Auth";
    o.IdleTimeout = TimeSpan.FromHours(8);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthMessageHandler>();

// === Main API client (Portal API) ===
builder.Services.AddHttpClient("ApiClient", (sp, http) =>
{
    var cfg = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
    http.BaseAddress = new Uri(cfg.ApiBaseUrl);
})
.AddHttpMessageHandler<AuthMessageHandler>();

// === IDW API client ===
builder.Services.AddHttpClient("IdwApiBaseUrl", (sp, http) =>
{
    var cfg = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
    http.BaseAddress = new Uri(cfg.IdwApiBaseUrl);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = false,
    AllowAutoRedirect = false,
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// === MDW API client (marketplace) ===
builder.Services.AddHttpClient("MdwApiBaseUrl", (sp, http) =>
{
    var cfg = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
    if (string.IsNullOrWhiteSpace(cfg.MdwApiBaseUrl))
        throw new InvalidOperationException("ApiSettings:MdwApiBaseUrl is not configured.");

    http.BaseAddress = new Uri(cfg.MdwApiBaseUrl);
    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = false,
    AllowAutoRedirect = false,
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// === Services ===
builder.Services.AddScoped<MdwMarketplaceService>();
builder.Services.AddScoped<IdwImportService>();
builder.Services.AddScoped<IdwListState>();
builder.Services.AddScoped<MiscService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<PartnerService>();
builder.Services.AddScoped<ShopService>();
builder.Services.AddScoped<AuthTokenProvider>();
builder.Services.AddScoped<UserSyncService>();
builder.Services.AddSingleton<TokenCacheService>();


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

// ===== Session login (ถูกเรียกจาก miniapp.setSession) =====

app.MapPost("/auth/set-session", async (HttpContext ctx, SetSessionDtos dto, TokenCacheService tokenService) =>
{
    if (dto is null || string.IsNullOrWhiteSpace(dto.Token))
        return Results.BadRequest("Invalid token");

    // เก็บลง Session ตามเดิม
    ctx.Session.SetString("JWT", dto.Token);
    ctx.Session.SetInt32("USERID", dto.UserId);
    ctx.Session.SetString("FULLNAME", dto.Fullname ?? "");
    ctx.Session.SetString("ROLE", dto.Role ?? "");
    if (!string.IsNullOrEmpty(dto.Password))
        ctx.Session.SetString("PWD", dto.Password);

    var claims = new List<System.Security.Claims.Claim>
    {
        new(System.Security.Claims.ClaimTypes.Name, dto.Username ?? ""),
        new(System.Security.Claims.ClaimTypes.Role, dto.Role ?? ""),
        new("ROLE", dto.Role ?? ""), // For UI display
        new("USERID", dto.UserId.ToString()),
        new("JWT", dto.Token),
        new("FULLNAME", dto.Fullname ?? ""),
        new("PASSWORD", dto.Password ?? "") 
    };

    // Sign in cookie
    var identity = new System.Security.Claims.ClaimsIdentity(claims, 
        CookieAuthenticationDefaults.AuthenticationScheme,
        System.Security.Claims.ClaimTypes.Name,
        System.Security.Claims.ClaimTypes.Role);

    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new System.Security.Claims.ClaimsPrincipal(identity),
        new AuthenticationProperties { IsPersistent = true });

    await ctx.Session.CommitAsync();

    // Eagerly fetch tokens to pre-warm the cache (Background Task to avoid blocking UI)
    // Pass args explicitly because Task.Run loses HttpContext
    Task.Run(async () => 
    {
        try
        {
            await tokenService.GetMdwTokenAsync(dto.Username, dto.Password);
            await tokenService.GetIdwTokenAsync(dto.Username, dto.Password);
        }
        catch { /* ignore */ }
    });

    return Results.Ok(new { success = true });
})
.AllowAnonymous()
.DisableAntiforgery();

// ===== Logout =====
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

    ctx.Response.Cookies.Delete("adwPortal.Auth", opts);
    ctx.Response.Cookies.Delete("adwPortal.AuthCookie", opts);

    return Results.Ok(new { success = true });
})
.AllowAnonymous()
.DisableAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();

// ======================
// Dtos set-session
// ======================
public record SetSessionDtos(string Token, int UserId, string? Fullname, string? Username, string? Password, string? Role);
