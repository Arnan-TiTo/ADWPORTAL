using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.WebOrders.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace miniApp.WebOrders.Pages
{
    public class HistoryModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public List<OrderHistoryViewDto> Orders { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Query { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ActiveTab { get; set; } = "History";

        [BindProperty]
        public List<int> SelectedIds { get; set; } = new();
        public HistoryModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task<IActionResult> OnPostExportCsv(string selectedIds)
        {
            if (string.IsNullOrWhiteSpace(selectedIds))
                return Content("No items selected.");

            var apiBase = _config["APIBASEURL"] ?? "";
            var token = _config["AUTHTOKEN"] ?? "";
            var client = _httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{apiBase}api/Order/history");
            var json = await response.Content.ReadAsStringAsync();
            var apiOrders = JsonSerializer.Deserialize<List<OrderHistoryViewDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var orders = apiOrders ?? new();

            // Parse selected index
            var selectedSet = new HashSet<(int orderIdx, int itemIdx)>();
            foreach (var pair in selectedIds.Split(",", StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split("-");
                if (parts.Length == 2 && int.TryParse(parts[0], out int o) && int.TryParse(parts[1], out int i))
                    selectedSet.Add((o, i));
            }

            // Compose csv
            var sb = new StringBuilder();
            sb.AppendLine("OrderNo,OrderDate,ProductId,ProductName,Quantity,UnitPrice,Discount,Total,ImageUrl");

            for (int orderIdx = 0; orderIdx < orders.Count; orderIdx++)
            {
                var order = orders[orderIdx];
                for (int itemIdx = 0; itemIdx < order.Items.Count; itemIdx++)
                {
                    if (!selectedSet.Contains((orderIdx, itemIdx)))
                        continue;

                    var item = order.Items[itemIdx];
                    sb.AppendLine($"\"{order.OrderNo}\",\"{order.OrderDate:yyyy-MM-dd HH:mm}\",\"{item.ProductId}\",\"{item.ProductName}\",{item.Quantity},{item.UnitPrice},{item.Discount},{item.Total},\"{item.ImageUrl}\"");
                }
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            var fileName = $"orders_{DateTime.Now:yyyyMMddHHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        public async Task OnGetAsync()
        {
            var apiBase = _config["APIBASEURL"] ?? "";
            var token = _config["AUTHTOKEN"] ?? "";

            var client = _httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"{apiBase}api/Order/history";
            if (!string.IsNullOrEmpty(Query))
                url += $"?query={Uri.EscapeDataString(Query)}";

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apiOrders = JsonSerializer.Deserialize<List<OrderHistoryViewDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Orders = apiOrders ?? new();
            }
        }
    }
}
