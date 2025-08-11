using Microsoft.Extensions.FileProviders;
using miniApp.AdminPortal.Components;
using miniApp.AdminPortal.Models;

namespace miniApp.AdminPortal
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var authToken = Environment.GetEnvironmentVariable("AuthToken", EnvironmentVariableTarget.Machine);
            builder.Services.AddSingleton(new AuthTokenProvider(authToken ?? ""));

            builder.Services.Configure<ApiSettings>(
                builder.Configuration.GetSection("ApiSettings")
            );

            builder.Services.AddHttpClient();

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

            app.UseStaticFiles();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            var iisRoot = builder.Configuration["ImageRootPath"];
            if (Directory.Exists(iisRoot))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(iisRoot),
                    RequestPath = "/images"
                });
            }

            app.Run();
        }
    }

    public class AuthTokenProvider
    {
        public string Token { get; }
        public AuthTokenProvider(string token) => Token = token;
    }
}
