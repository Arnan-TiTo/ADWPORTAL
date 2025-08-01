using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace miniApp.Web.Services
{
    public class ApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _config;

        public ApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _config = config;
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _config["ApiBaseUrl"] ?? "";
            client.BaseAddress = new Uri(baseUrl);

            // Add JWT token if exists
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JWT");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        public async Task<T?> GetAsync<T>(string url)
        {
            var client = CreateClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<T?> PostAsync<T>(string url, object data)
        {
            var client = CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<HttpResponseMessage> PostFormAsync(string url, MultipartFormDataContent form)
        {
            var client = CreateClient();
            return await client.PostAsync(url, form);
        }

        public async Task<T?> PutAsync<T>(string url, object data)
        {
            var client = CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

            var response = await client.PutAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<bool> DeleteAsync(string url)
        {
            var client = CreateClient();
            var response = await client.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
    }
}
