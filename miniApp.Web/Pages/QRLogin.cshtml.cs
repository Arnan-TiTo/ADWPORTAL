using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace miniApp.Web.Pages
{
    public class QRLoginModel : PageModel
    {
        private readonly ILogger<QRLoginModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        public string ApiBaseUrl { get; set; } = "";
        public string? QrToken { get; set; }
        public string? QrImageUrl { get; set; }

        public QRLoginModel(ILogger<QRLoginModel> logger, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public void OnGet()
        {
            ApiBaseUrl = _config["ApiBaseUrl"] ?? "";
        }
        public async Task<IActionResult> OnGetAsync()
        {
            var apiBase = _config["ApiBaseUrl"] ?? "";
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync($"{apiBase}api/qrlogin/generate");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to generate QR login token");
                return RedirectToPage("/Login");
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<GenerateQrResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            QrToken = data?.QrToken;
            QrImageUrl = $"{apiBase}api/qrlogin/image?token={QrToken}"; 

            return Page();
        }

        public class GenerateQrResponse
        {
            public string? QrToken { get; set; }
        }
    }
}
