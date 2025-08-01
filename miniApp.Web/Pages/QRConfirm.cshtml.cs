using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text;
using System.Net.Http;

namespace miniApp.Web.Pages
{
    public class QRConfirmModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public QRConfirmModel(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public string QrToken { get; set; } = "";

        [BindProperty]
        public LoginDto Login { get; set; } = new();

        public string? Message { get; set; }

        public void OnGet()
        {
            if (string.IsNullOrEmpty(QrToken))
            {
                Message = "QR token not provided.";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(QrToken))
            {
                Message = "Missing QR token.";
                return Page();
            }

            var payload = new
            {
                qrToken = QrToken,
                username = Login.Username,
                password = Login.Password
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient();
            var apiBase = _config["ApiBaseUrl"] ?? "";

            var response = await client.PostAsync($"{apiBase}api/qrlogin/confirm", content);
            if (response.IsSuccessStatusCode)
            {
                Message = "Login confirmed! You may now return to desktop.";
            }
            else
            {
                Message = "Failed to confirm. Please check credentials or try again.";
            }

            return Page();
        }

        public class LoginDto
        {
            [Required]
            public string Username { get; set; } = "";

            [Required]
            public string Password { get; set; } = "";
        }
    }
}
