using miniApp.Web.Middlewares;
using miniApp.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;  // กำหนดให้ cookie สามารถเข้าถึงได้จาก server-side เท่านั้น
    options.IdleTimeout = TimeSpan.FromMinutes(30);  // ระยะเวลาใช้งาน session
});

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
builder.Services.AddScoped<miniApp.Web.Services.AuthService>();
builder.Services.AddScoped<miniApp.Web.Services.LocationService>();


var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();           
app.UseAuthorization();

app.UseMiddleware<AuthGuardMiddleware>();

app.MapRazorPages();

app.Run();
