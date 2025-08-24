using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.WebOrders.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;

namespace miniApp.WebOrders.Pages
{
    public class ProductSearchModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public ProductSearchModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }

        [BindProperty]
        public int LocationId { get; set; }

        public List<ProductDto> Products { get; set; } = new();
        public List<ProductCategoryDto> Categories { get; set; } = new();

        public HashSet<int> VisibleProductIds { get; set; } = new();

        public async Task OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient();

            var baseUrl = _config["ApiBaseUrl"] ?? "";
            var token = _config["AUTHTOKEN"] ?? "";
            var userId = HttpContext.Session.GetInt32("USERID") ?? 0;

            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            ViewData["USERID"] = userId;
            ViewData["APIBASEURL"] = baseUrl;
            ViewData["AUTHTOKEN"] = token;

            Categories = await client.GetFromJsonAsync<List<ProductCategoryDto>>(
                $"{baseUrl}api/productcategory") ?? new();

            // 1) หา location ที่ user ได้สิทธิ์
            var allowLocations = new List<UserLocationDto>();
            if (userId > 0)
            {
                allowLocations = await client.GetFromJsonAsync<List<UserLocationDto>>(
                    $"{baseUrl}api/userlocations/user/{userId}") ?? new();
            }

            if (allowLocations.Count == 0)
            {
                Products = new();
                VisibleProductIds = new();
                return;
            }

            // 2) รวม product ที่มี stock ในแต่ละ location
            var visible = new HashSet<int>();
            foreach (var loc in allowLocations)
            {
                var stocks = await client.GetFromJsonAsync<List<StockRow>>(
                    $"{baseUrl}api/ProductStock/location/{loc.LocationId}?userId={userId}") ?? new();

                foreach (var s in stocks)
                    visible.Add(s.ProductId);

                // ตั้งค่า LocationId ค่าเริ่มต้นเป็น location แรก
                if (LocationId == 0) LocationId = loc.LocationId;
            }
            VisibleProductIds = visible;

            // 3) โหลดสินค้า (ตามหมวดหรือทั้งหมด) แล้ว filter ด้วย VisibleProductIds
            string url = CategoryId.HasValue
                ? $"{baseUrl}api/product/ByCategory?categoryId={CategoryId.Value}"
                : $"{baseUrl}api/product";

            var all = await client.GetFromJsonAsync<List<ProductDto>>(url) ?? new();
            Products = all.Where(p => visible.Contains(p.Id)).ToList();
        }

        [ValidateAntiForgeryToken]
        public IActionResult OnPostAddToCart(int productId, int locationId)
        {
            // รับ LocationId จากฟอร์ม (ถ้า 0 ให้ fallback เป็นค่าบน Model)
            var loc = locationId > 0 ? locationId : LocationId;

            var productName = Request.Form["ProductName"].ToString();
            var imageUrl = Request.Form["ImageUrl"].ToString();
            decimal.TryParse(Request.Form["Price"], out var price);

            var cart = GetCart();
            var existing = cart.Find(p => p.ProductId == productId && p.LocationId == loc);
            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                cart.Add(new CartItemDto
                {
                    LocationId = loc,
                    ProductId = productId,
                    ProductName = productName,
                    Price = price,
                    ImageUrl = imageUrl,
                    Quantity = 1
                });
            }

            SaveCart(cart);
            TempData["ShowToast"] = $"เพิ่มสินค้า \"{productName}\" ลงตะกร้าเรียบร้อยแล้ว";
            return RedirectToPage();
        }

        // ===== Helpers =====
        private List<CartItemDto> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            return cartJson != null
                ? JsonSerializer.Deserialize<List<CartItemDto>>(cartJson)!
                : new List<CartItemDto>();
        }

        private void SaveCart(List<CartItemDto> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("Cart", cartJson);
        }

        // ==== DTOs สำหรับเรียก API เฉพาะที่หน้าจอนี้ต้องใช้ ====
        public class UserLocationDto
        {
            public int UserId { get; set; }
            public int LocationId { get; set; }
        }

        public class StockRow
        {
            public int ProductId { get; set; }
            public int QtyAvailable { get; set; }
        }
    }
}
