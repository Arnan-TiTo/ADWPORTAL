using miniApp.WebOrders.Dtos;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace miniApp.WebOrders.Services
{
    public class ProductService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public ProductService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<List<ProductResponseDto>> GetAllProductsAsync(string token)
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _http.GetAsync("api/ProductSearch");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<ProductResponseDto>>();
                return result ?? new();
            }

            return new();
        }
    }
}
