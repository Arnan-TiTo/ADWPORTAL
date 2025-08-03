using miniApp.AdminPortal.Components;
using miniApp.AdminPortal.Models;

namespace miniApp.AdminPortal
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // อ่าน AuthToken จาก Environment Variable
            var authToken = Environment.GetEnvironmentVariable("AuthToken", EnvironmentVariableTarget.Machine);
            builder.Services.AddSingleton(new AuthTokenProvider(authToken ?? ""));

            // โหลด ApiSettings
            builder.Services.Configure<ApiSettings>(
                builder.Configuration.GetSection("ApiSettings")
            );

            // เพิ่ม HttpClient BaseAddress
            builder.Services.AddHttpClient();

            // Razor Components
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }

    public class AuthTokenProvider
    {
        public string Token { get; }
        public AuthTokenProvider(string token) => Token = token;
    }
}
