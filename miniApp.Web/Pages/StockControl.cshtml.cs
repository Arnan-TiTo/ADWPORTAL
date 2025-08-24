using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace miniApp.Web.Pages
{
    public class ProductModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public ProductModel(IHttpClientFactory httpClientFactory, IConfiguration config)
            => (_httpClientFactory, _config) = (httpClientFactory, config);

        // ===== Query Params / Binding =====
        [BindProperty(Name = "query", SupportsGet = true)] public string? SearchQuery { get; set; }
        [BindProperty(Name = "sort", SupportsGet = true)] public string SortOrder { get; set; } = "asc";
        [BindProperty(Name = "locationId", SupportsGet = true)] public int? LocationId { get; set; }

        // ===== View Data =====
        public int CurrentUserId { get; set; }
        public bool IsCurrentStorehouse { get; set; }
        public List<LocationOption> Locations { get; set; } = new();
        public List<ProductDto> Products { get; set; } = new();

        public async Task OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var apiBase = (_config["APIBASEURL"] ?? "").TrimEnd('/') + "/";
            var token = _config["AUTHTOKEN"] ?? "";
            var userId = HttpContext.Session.GetInt32("USERID") ?? 0;
            CurrentUserId = userId;

            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            ViewData["APIBASEURL"] = apiBase;
            ViewData["AUTHTOKEN"] = token;
            ViewData["USERID"] = userId;

            // 1) สิทธิ์ location ของ user
            var allows = new List<UserLocationDto>();
            if (userId > 0)
                allows = await client.GetFromJsonAsync<List<UserLocationDto>>(
                    $"{apiBase}api/userlocations/user/{userId}") ?? new();

            var allowIds = allows.Select(a => a.LocationId).ToHashSet();

            // 2) dropdown location (กรองด้วยสิทธิ์)
            var drops = await client.GetFromJsonAsync<List<LocationOption>>(
                $"{apiBase}api/locations/dropdown") ?? new();

            Locations = drops
                .Where(d => allowIds.Count == 0 || allowIds.Contains(d.Id))
                .OrderBy(d => d.Name)
                .ToList();

            if (Locations.Count > 0 && (!LocationId.HasValue || LocationId <= 0))
                LocationId = Locations.First().Id;

            if (LocationId.HasValue && allowIds.Count > 0 && !allowIds.Contains(LocationId.Value))
            {
                Products = new();
                return;
            }

            // 3) อ่าน ProductStocks ของ location ที่เลือก
            var stockMap = new Dictionary<int, StockRow>();
            HashSet<int> locProductIds = new();
            if (LocationId is int locId && locId > 0)
            {
                var stocks = await client.GetFromJsonAsync<List<StockRow>>(
                    $"{apiBase}api/ProductStock/location/{locId}?userId={userId}") ?? new();

                stockMap = stocks.ToDictionary(s => s.ProductId, s => s);
                locProductIds = stocks.Select(s => s.ProductId).ToHashSet();

                // ใช้ flag ของ Location จากผลลัพธ์นี้
                IsCurrentStorehouse = stocks.FirstOrDefault()?.IsStoreHouse == 1;
            }

            // 4) โหลดสินค้า → กรองเฉพาะที่มีใน stock ของ location นี้ + ค้นหา + sort
            var all = await client.GetFromJsonAsync<List<ProductDto>>($"{apiBase}api/product") ?? new();

            IEnumerable<ProductDto> q = all;
            if (LocationId is int _l && _l > 0)
                q = locProductIds.Count > 0 ? q.Where(p => locProductIds.Contains(p.Id))
                                            : Enumerable.Empty<ProductDto>();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var k = SearchQuery.Trim();
                q = q.Where(p =>
                    (p.Name ?? "").Contains(k, StringComparison.OrdinalIgnoreCase) ||
                    (p.Sku ?? "").Contains(k, StringComparison.OrdinalIgnoreCase));
            }

            q = SortOrder?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true
                ? q.OrderByDescending(p => p.Name)
                : q.OrderBy(p => p.Name);

            Products = q.ToList();

            // 5) อัดค่าจาก ProductStocks ลง ProductDto
            foreach (var p in Products)
            {
                if (stockMap.TryGetValue(p.Id, out var s))
                {
                    p.OnHand = s.QtyOnHand;
                    p.Available = s.QtyAvailable;
                    p.Reserved = s.QtyReserved;   // เก็บไว้เป็นข้อมูลพื้นฐาน (ไม่แสดงหน้า UI)
                    p.Damaged = s.QtyDamaged;
                    p.Receive = s.QtyReceive;    // แสดงแทน Reserved
                }
                else
                {
                    p.OnHand = p.Available = p.Reserved = p.Damaged = p.Receive = 0;
                }
            }
        }

        // ===== DTOs =====
        public class LocationOption
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public int IsWareHouse { get; set; }    // optional: สำหรับอนาคต
            public int IsStoreHouse { get; set; }   // optional: สำหรับอนาคต
        }

        public class UserLocationDto { public int UserId { get; set; } public int LocationId { get; set; } }

        // ดึงจาก ProductStock/location
        public class StockRow
        {
            public int ProductId { get; set; }
            public int QtyOnHand { get; set; }
            public int QtyReserved { get; set; }
            public int QtyDamaged { get; set; }
            public int QtyAvailable { get; set; }
            public int QtyReceive { get; set; }     // NEW
            public int IsWareHouse { get; set; }    // flag ของ location (มาจาก join)
            public int IsStoreHouse { get; set; }   // flag ของ location (มาจาก join)
        }

        public class ProductDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string? Sku { get; set; }
            public string? Description { get; set; }
            public int Quantity { get; set; }
            public string? Note { get; set; }
            public string? ImageUrl { get; set; }
            public DateTime? CreatedAt { get; set; }
            public int LocationId { get; set; }
            public int UserId { get; set; }
            public string? UserFullname { get; set; }

            // From ProductStocks
            public int OnHand { get; set; }
            public int Available { get; set; }
            public int Reserved { get; set; }
            public int Damaged { get; set; }
            public int Receive { get; set; }
        }
    }
}
