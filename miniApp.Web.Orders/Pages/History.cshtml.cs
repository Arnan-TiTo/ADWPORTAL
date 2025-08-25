using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.WebOrders.Models;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace miniApp.WebOrders.Pages
{
    public class HistoryModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public List<OrderHistoryViewDto> Orders { get; set; } = new();
        [BindProperty(SupportsGet = true)] public string? Query { get; set; }
        [BindProperty(SupportsGet = true)] public string? ActiveTab { get; set; } = "History";
        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
        [BindProperty(SupportsGet = true)] public bool ShowFilter { get; set; }

        [BindProperty(SupportsGet = true)] public string SortDir { get; set; } = "desc";

        public string NextSortDir =>
            SortDir?.Equals("asc", StringComparison.OrdinalIgnoreCase) == true ? "desc" : "asc";


        public HistoryModel(IHttpClientFactory httpClientFactory, IConfiguration config)
            => (_httpClientFactory, _config) = (httpClientFactory, config);

        public async Task OnGetAsync()
        {
            var client = BuildApiClient(out var apiBase);

            // สิทธิ์ location ของผู้ใช้
            var userId = HttpContext.Session.GetInt32("USERID") ?? 0;
            var allowed = await GetAllowedLocationIdsAsync(client, apiBase, userId);
            if (allowed.Count == 0) { Orders = new(); return; }

            // เรียก API: history/by-location (กรองช่วงวัน & query)
            var url = new StringBuilder($"{apiBase}api/Order/history/by-location?userId={userId}");
            if (FromDate.HasValue) url.Append($"&from={FromDate.Value:yyyy-MM-dd}");
            if (ToDate.HasValue) url.Append($"&to={ToDate.Value:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(Query))
                url.Append($"&q={Uri.EscapeDataString(Query.Trim())}");

            var resp = await client.GetAsync(url.ToString());
            if (!resp.IsSuccessStatusCode) { Orders = new(); return; }

            var json = await resp.Content.ReadAsStringAsync();
            Orders = JsonSerializer.Deserialize<List<OrderHistoryViewDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            Orders = (SortDir?.ToLowerInvariant()) switch
            {
                "asc" => Orders.OrderBy(o => o.OrderDate).ToList(),
                _ => Orders.OrderByDescending(o => o.OrderDate).ToList(),
            };
        }

        // -------- Helpers --------
        private HttpClient BuildApiClient(out string apiBase)
        {
            apiBase = (_config["APIBASEURL"] ?? "").TrimEnd('/') + "/";
            var token = _config["AUTHTOKEN"] ?? "";
            var client = _httpClientFactory.CreateClient();
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<HashSet<int>> GetAllowedLocationIdsAsync(HttpClient client, string apiBase, int userId)
        {
            if (userId <= 0) return new();
            try
            {
                var json = await client.GetStringAsync($"{apiBase}api/userlocations/user/{userId}");
                var items = JsonSerializer.Deserialize<List<UserLocDto>>(json,
                             new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                return items.Select(x => x.LocationId).ToHashSet();
            }
            catch { return new(); }
        }
        private sealed class UserLocDto
        {
            public int UserId { get; set; }
            public int LocationId { get; set; }
        }
    }
}
