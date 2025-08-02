using Microsoft.Extensions.Configuration;
using miniApp.Web.Dtos;
using miniApp.Web.Pages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Tasks;
using static miniApp.Web.Pages.RegisterModel;

namespace miniApp.Web.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public AuthService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;

            var apiBase = _config["ApiBaseUrl"] ?? "";
            _httpClient.BaseAddress = new System.Uri(apiBase);
        }

        public async Task<(bool Success, string Error)> RegisterAsync(UserRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, content);

            return (true, "");
        }


        public async Task<LoginResponse?> LoginAsync(LoginRequest login)
        {
            var response = await _httpClient.PostAsJsonAsync("api/Auth/login", login);

            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return result;
        }


    }
}
