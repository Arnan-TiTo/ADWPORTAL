using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using miniApp.WebOrders.Middlewares;
using miniApp.WebOrders.Services;

var builder = WebApplication.CreateBuilder(args);
var authToken = builder.Configuration["AuthToken"];
var config = builder.Configuration;

builder.Configuration.AddEnvironmentVariables();

// Services
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.Cookie.Name = "MiniApp.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; //https
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    });

// DI
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<LocationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseStaticFiles();

var iisRoot = builder.Configuration["ImageRootPath"];
if (Directory.Exists(iisRoot))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(iisRoot),
        RequestPath = "/images"
    });
}

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<AuthGuardMiddleware>();

app.MapRazorPages();

app.Run();
