using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace miniApp.Web.Pages
{
    public class ReturnDamageModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public ReturnDamageModel(IHttpClientFactory httpClientFactory, IConfiguration config)
            => (_httpClientFactory, _config) = (httpClientFactory, config);

        // Query/Binding
        [BindProperty(SupportsGet = true, Name = "locationId")]
        public int? LocationId { get; set; }

        // View Data
        public int CurrentUserId { get; set; }
        public List<LocationOption> Storehouses { get; set; } = new();
        public List<ProductChoice> ProductChoices { get; set; } = new();

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

            // 1) ดึงสิทธิ์ Location ของ user
            var allows = new List<UserLocationDto>();
            if (userId > 0)
            {
                allows = await client.GetFromJsonAsync<List<UserLocationDto>>(
                    $"{apiBase}api/userlocations/user/{userId}") ?? new();
            }
            var allowIds = allows.Select(a => a.LocationId).ToHashSet();

            // 2) ดึง dropdown locations แล้วกรองเฉพาะที่เป็น Storehouse และ user มีสิทธิ์
            var drops = await client.GetFromJsonAsync<List<LocationOption>>(
                $"{apiBase}api/locations/dropdown") ?? new();

            // NOTE: ต้องมีฟิลด์ isStorehouse ใน endpoint /api/locations/dropdown ด้วย ถ้ายังไม่มี ให้ทำ endpoint แยกหรือโหลดรายละเอียด location เพิ่ม
            // ที่นี่ขอกรองด้วยชื่อชั่วคราว: ถ้า API dropdown ไม่มี flag, คุณแก้ endpoint ให้แนบ isStorehouse มาด้วยจะดีที่สุด
            // เวอร์ชันนี้ fallback: เอาทุก location ที่ user มีสิทธิ์ แล้วค่อยไปตรวจ filter ที่ฝั่ง Stock
            Storehouses = drops
                .Where(d => allowIds.Count == 0 || allowIds.Contains(d.Id))
                .OrderBy(d => d.Name)
                .ToList();

            if (Storehouses.Count > 0 && (!LocationId.HasValue || LocationId <= 0))
                LocationId = Storehouses.First().Id;

            // 3) โหลด stock ของ location ที่เลือก แล้วทำ choices เฉพาะสินค้าที่ QtyDamaged > 0
            ProductChoices = new();
            if (LocationId is int locId && locId > 0)
            {
                var stocks = await client.GetFromJsonAsync<List<StockRow>>(
                    $"{apiBase}api/ProductStock/location/{locId}?userId={userId}") ?? new();

                ProductChoices = stocks
                    .Where(s => s.QtyDamaged > 0) // ต้องมีของเสีย
                    .OrderBy(s => s.Name)
                    .Select(s => new ProductChoice
                    {
                        ProductId = s.ProductId,
                        Name = s.Name ?? $"Product #{s.ProductId}",
                        Sku = s.Sku ?? "",
                        QtyDamaged = s.QtyDamaged,
                        QtyOnHand = s.QtyOnHand
                    })
                    .ToList();
            }
        }

        // ===== DTO/View models =====
        public class LocationOption { public int Id { get; set; } public string Name { get; set; } = ""; }
        public class UserLocationDto { public int UserId { get; set; } public int LocationId { get; set; } }

        public class StockRow
        {
            public int ProductId { get; set; }
            public string? Name { get; set; }
            public string? Sku { get; set; }
            public int QtyOnHand { get; set; }
            public int QtyReserved { get; set; }
            public int QtyDamaged { get; set; }
            public int QtyAvailable { get; set; }
            public int QtyReceive { get; set; }
        }

        public class ProductChoice
        {
            public int ProductId { get; set; }
            public string Name { get; set; } = "";
            public string Sku { get; set; } = "";
            public int QtyDamaged { get; set; }
            public int QtyOnHand { get; set; }
        }
    }
}
