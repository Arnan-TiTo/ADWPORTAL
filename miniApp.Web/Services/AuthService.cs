using Microsoft.Extensions.Configuration;
using miniApp.API.Dtos;
using System.Net.Http;
using System.Net.Http.Json;
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

            var apiBase = _config["ApiBaseUrl"] ?? "http://localhost:5252";
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


        public async Task<string?> LoginAsync(object loginModel)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginModel);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadFromJsonAsync<AuthResponse>();
            return json?.Token;
        }

        private class AuthResponse
        {
            public string? Token { get; set; }
        }
    }
}
