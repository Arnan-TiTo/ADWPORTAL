using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;

namespace miniApp.WebOrders.Pages
{
    public class ProductListModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _cfg;
        public ProductListModel(IHttpClientFactory http, IConfiguration cfg)
            => (_http, _cfg) = (http, cfg);

        public int LocationId { get; private set; }
        public string? Query { get; set; }

        public string LocationName { get; set; } = "-";
        public List<Row> Items { get; set; } = new();

        // view row
        public class UserLocationDto
        {
            public int UserId { get; set; }
            public int LocationId { get; set; }
        }

        public class Row
        {
            public int ProductId { get; set; }
            public string Name { get; set; } = "";
            public string Sku { get; set; } = "";
            public string? ImageUrl { get; set; }
            public int Available { get; set; }
        }

        // api row
        private class ApiRow
        {
            public int ProductId { get; set; }
            public string? Name { get; set; }
            public string? Sku { get; set; }
            public string? ImageUrl { get; set; }
            public int QtyOnHand { get; set; }
            public int QtyReserved { get; set; }
            public int QtyDamaged { get; set; }
            public int QtyAvailable { get; set; }
            public int LocationId { get; set; }
            public string? LocationName { get; set; }
        }

        public async Task OnGetAsync()
        {
            var apiBase = (_cfg["APIBASEURL"] ?? "").TrimEnd('/') + "/";
            var token = _cfg["AUTHTOKEN"] ?? "";
            var userId = HttpContext.Session.GetInt32("USERID") ?? 0;
            if (userId <= 0) return;

            var client = _http.CreateClient();
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);


            // อ่านเป็น DTO แล้วดึง LocationId
            var allows = await client.GetFromJsonAsync<List<UserLocationDto>>(
                $"{apiBase}api/userlocations/user/{userId}") ?? new();

            var allowIds = allows.Select(a => a.LocationId).ToList();

            var sessionLoc = HttpContext.Session.GetInt32("LOCATIONID") ?? 0;
            if (sessionLoc > 0 && allowIds.Contains(sessionLoc))
                LocationId = sessionLoc;
            else
                LocationId = allowIds.FirstOrDefault();   // ใช้ตัวแรกของผู้ใช้

            if (LocationId <= 0)
            {
                LocationName = "- (ไม่มีสิทธิ์สถานที่)";
                Items = new();
                return;
            }

            // โหลดสินค้าเฉพาะ location ของผู้ใช้
            var url = $"{apiBase}api/ProductStock/location/{LocationId}?userId={userId}";
            var rows = await client.GetFromJsonAsync<List<ApiRow>>(url) ?? new();

            if (rows.Count > 0)
                LocationName = rows.First().LocationName ?? $"- ({LocationId})";

            if (!string.IsNullOrWhiteSpace(Query))
            {
                var q = Query.Trim();
                rows = rows.Where(r =>
                    (r.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (r.Sku ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            Items = rows.Select(r =>
            {
                var avail = r.QtyAvailable;
                if (avail <= 0) avail = r.QtyOnHand - r.QtyReserved - r.QtyDamaged;
                if (avail < 0) avail = 0;
                return new Row
                {
                    ProductId = r.ProductId,
                    Name = r.Name ?? $"Product #{r.ProductId}",
                    Sku = r.Sku ?? "-",
                    ImageUrl = r.ImageUrl,
                    Available = avail
                };
            })
            .OrderBy(r => r.Name)
            .ToList();
        }


    }
}
