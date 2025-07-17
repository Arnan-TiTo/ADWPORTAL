using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.WebOrders.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Linq;

namespace miniApp.WebOrders.Pages
{
    public class PurchaseHistoryModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public List<OrderHistoryViewDto> Orders { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string Query { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string ProductFilter { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string OrderNoFilter { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string UnitPriceFilter { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string SortField { get; set; } = "OrderNo";

        [BindProperty(SupportsGet = true)]
        public string SortDir { get; set; } = "asc";

        [BindProperty(SupportsGet = true)]
        public bool ShowFilter { get; set; } = false;

        public List<(string Value, string Label)> SortFields { get; set; } = new()
        {
            ("OrderNo", "Order No"),
            ("OrderDate", "Order Date"),
            ("CustomerName", "Customer Name"),
            ("ProductName", "Product Name"),
            ("UnitPrice", "Unit Price"),
        };

        public PurchaseHistoryModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task OnGetAsync()
        {
            var apiBase = _config["APIBASEURL"] ?? "http://localhost:5252";
            var token = _config["AUTHTOKEN"] ?? "";

            var client = _httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{apiBase}/api/Order/history");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apiOrders = JsonSerializer.Deserialize<List<OrderHistoryViewDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                // FILTER
                IEnumerable<OrderHistoryViewDto> filteredOrders = apiOrders;

                // Search by OrderNo
                if (!string.IsNullOrWhiteSpace(Query))
                    filteredOrders = filteredOrders.Where(o => (o.OrderNo ?? "").Contains(Query, StringComparison.OrdinalIgnoreCase));

                // Filter by ProductName
                if (!string.IsNullOrWhiteSpace(ProductFilter))
                {
                    filteredOrders = filteredOrders
                        .Select(o => new OrderHistoryViewDto
                        {
                            OrderNo = o.OrderNo,
                            OrderDate = o.OrderDate,
                            CustomerName = o.CustomerName,
                            AddressLine = o.AddressLine,
                            SubDistrict = o.SubDistrict,
                            District = o.District,
                            Province = o.Province,
                            ZipCode = o.ZipCode,
                            Items = o.Items.Where(i => (i.ProductName ?? "").Contains(ProductFilter, StringComparison.OrdinalIgnoreCase)).ToList()
                        })
                        .Where(o => o.Items.Any());
                }

                // Filter by OrderNo
                if (!string.IsNullOrWhiteSpace(OrderNoFilter))
                    filteredOrders = filteredOrders.Where(o => (o.OrderNo ?? "").Contains(OrderNoFilter, StringComparison.OrdinalIgnoreCase));

                // Filter by UnitPrice
                if (!string.IsNullOrWhiteSpace(UnitPriceFilter) && decimal.TryParse(UnitPriceFilter, out var unitPrice))
                {
                    filteredOrders = filteredOrders
                        .Select(o => new OrderHistoryViewDto
                        {
                            OrderNo = o.OrderNo,
                            OrderDate = o.OrderDate,
                            CustomerName = o.CustomerName,
                            AddressLine = o.AddressLine,
                            SubDistrict = o.SubDistrict,
                            District = o.District,
                            Province = o.Province,
                            ZipCode = o.ZipCode,
                            Items = o.Items.Where(i => i.UnitPrice == unitPrice).ToList()
                        })
                        .Where(o => o.Items.Any());
                }

                // ORDER BY
                bool descending = SortDir == "desc";
                switch (SortField?.ToLower())
                {
                    case "orderdate":
                        filteredOrders = descending ? filteredOrders.OrderByDescending(o => o.OrderDate) : filteredOrders.OrderBy(o => o.OrderDate);
                        break;
                    case "customername":
                        filteredOrders = descending ? filteredOrders.OrderByDescending(o => o.CustomerName ?? "") : filteredOrders.OrderBy(o => o.CustomerName ?? "");
                        break;
                    case "productname":
                        filteredOrders = descending
                            ? filteredOrders.OrderByDescending(o => o.Items.FirstOrDefault()?.ProductName ?? "")
                            : filteredOrders.OrderBy(o => o.Items.FirstOrDefault()?.ProductName ?? "");
                        break;
                    case "unitprice":
                        filteredOrders = descending
                            ? filteredOrders.OrderByDescending(o => o.Items.FirstOrDefault()?.UnitPrice ?? 0)
                            : filteredOrders.OrderBy(o => o.Items.FirstOrDefault()?.UnitPrice ?? 0);
                        break;
                    default:
                        filteredOrders = descending ? filteredOrders.OrderByDescending(o => o.OrderNo ?? "") : filteredOrders.OrderBy(o => o.OrderNo ?? "");
                        break;
                }

                Orders = filteredOrders.ToList();
            }
        }

        public async Task<IActionResult> OnPostDownloadAsync()
        {
            await OnGetAsync(); // อัปเดต Orders ให้ตรง filter/sort

            var sb = new StringBuilder();
            sb.AppendLine("OrderNo,OrderDate,CustomerName,Address,ProductName,Quantity,UnitPrice,Discount,Total");

            foreach (var order in Orders)
            {
                var address = $"{order.AddressLine}, {order.SubDistrict}, {order.District}, {order.Province}, {order.ZipCode}".Replace(", ,", ",").Trim(',', ' ');
                foreach (var item in order.Items)
                {
                    sb.AppendLine($"\"{order.OrderNo}\",\"{order.OrderDate:dd/MM/yyyy}\",\"{order.CustomerName}\",\"{address}\",\"{item.ProductName}\",{item.Quantity},{item.UnitPrice},{item.Discount},{item.UnitPrice - item.Discount}");
                }
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            var fileName = $"purchase_history_{DateTime.Now:yyyyMMddHHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }
    }
}
