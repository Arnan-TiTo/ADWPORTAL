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
});

builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.Cookie.Name = "MiniApp.Auth";
    });

// DI
builder.Services.AddScoped<miniApp.WebOrders.Services.AuthService>();
builder.Services.AddScoped<miniApp.WebOrders.Services.LocationService>();


var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];

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
