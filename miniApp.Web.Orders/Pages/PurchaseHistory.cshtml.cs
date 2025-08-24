using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using miniApp.WebOrders.Models;

namespace miniApp.WebOrders.Pages
{
    public class PurchaseHistoryModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public PurchaseHistoryModel(IHttpClientFactory http, IConfiguration config)
            => (_http, _config) = (http, config);

        [BindProperty(SupportsGet = true)] public string? Query { get; set; }
        [BindProperty(SupportsGet = true)] public string SortField { get; set; } = "OrderDate";
        [BindProperty(SupportsGet = true)] public string SortDir { get; set; } = "desc";
        [BindProperty(SupportsGet = true)] public bool ShowFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? ProductFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? OrderNoFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? UnitPriceFilter { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }

        public List<(string value, string label)> SortFields { get; set; } =
            new() { ("OrderDate", "Order Date"), ("OrderNo", "Order No"), ("CustomerName", "Customer") };

        public List<OrderHistoryViewDto> Orders { get; set; } = new();

        public async Task OnGetAsync()
        {
            var data = await LoadOrdersAsync();

            // filter ฝั่ง UI เพิ่มเติม (คำสั่ง/สินค้า/ราคา/ช่วงวัน)
            if (!string.IsNullOrWhiteSpace(OrderNoFilter))
                data = data.Where(o => (o.OrderNo ?? "")
                    .Contains(OrderNoFilter.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrWhiteSpace(ProductFilter))
                data = data.Where(o => o.Items.Any(i => (i.ProductName ?? "")
                    .Contains(ProductFilter.Trim(), StringComparison.OrdinalIgnoreCase))).ToList();

            if (decimal.TryParse(UnitPriceFilter, out var up))
                data = data.Where(o => o.Items.Any(i => i.UnitPrice == up)).ToList();

            if (FromDate.HasValue)
                data = data.Where(o => o.OrderDate >= FromDate.Value.Date).ToList();
            if (ToDate.HasValue)
                data = data.Where(o => o.OrderDate < ToDate.Value.Date.AddDays(1)).ToList();

            // sort
            data = (SortField, (SortDir ?? "desc").ToLower()) switch
            {
                ("OrderNo", "asc") => data.OrderBy(o => o.OrderNo).ToList(),
                ("OrderNo", "desc") => data.OrderByDescending(o => o.OrderNo).ToList(),
                ("CustomerName", "asc") => data.OrderBy(o => o.CustomerName).ToList(),
                ("CustomerName", "desc") => data.OrderByDescending(o => o.CustomerName).ToList(),
                ("OrderDate", "asc") => data.OrderBy(o => o.OrderDate).ToList(),
                _ => data.OrderByDescending(o => o.OrderDate).ToList(),
            };

            Orders = data;
        }

        private async Task<List<OrderHistoryViewDto>> LoadOrdersAsync()
        {
            var client = _http.CreateClient();
            var api = (_config["APIBASEURL"] ?? "").TrimEnd('/') + "/";
            var token = _config["AUTHTOKEN"] ?? "";
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

            var userId = HttpContext.Session.GetInt32("USERID") ?? 0;
            if (userId <= 0) return new();

            var from = (FromDate ?? DateTime.Today.AddDays(-90)).Date;
            var to = (ToDate ?? DateTime.Today).Date;

            var url = $"{api}api/order/history/by-location?userId={userId}" +
                      $"&from={Uri.EscapeDataString(from.ToString("O"))}" +
                      $"&to={Uri.EscapeDataString(to.ToString("O"))}";
            if (!string.IsNullOrWhiteSpace(Query))
                url += $"&q={Uri.EscapeDataString(Query!)}";

            List<OrderDto>? raw;
            try
            {
                raw = await client.GetFromJsonAsync<List<OrderDto>>(url);
            }
            catch
            {
                return new();
            }

            // map → VM
            var result = (raw ?? new()).Select(o => new OrderHistoryViewDto
            {
                OrderNo = o.OrderNo,
                OrderDate = o.OrderDate,
                CustomerName = o.CustomerName,
                AddressLine = o.AddressLine,
                SubDistrict = o.SubDistrict,
                District = o.District,
                Province = o.Province,
                ZipCode = o.ZipCode,
                Items = (o.Items ?? new()).Select(i => new OrderItemHistoryDto
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Discount = i.Discount
                }).ToList()
            }).ToList();

            return result;
        }

        // ===== DTOs =====

    }
}