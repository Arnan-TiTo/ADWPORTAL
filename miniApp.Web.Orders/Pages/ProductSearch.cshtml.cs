using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.WebOrders.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
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

        public List<ProductDto> Products { get; set; } = new();
        public List<ProductCategoryDto> Categories { get; set; } = new();

        // ชุด product id ที่ user มองเห็น (สินค้าที่อยู่ใน ProductStocks ของทุก location ที่ user ถูก assign)
        public HashSet<int> VisibleProductIds { get; set; } = new();

        public async Task OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient();

            // อ่านค่าจาก Session (มาจากหน้า Login)
            var baseUrl = _config["ApiBaseUrl"] ?? "";
            var token = _config["AUTHTOKEN"] ?? "";
            var userId = HttpContext.Session.GetInt32("USERID") ?? 0;

            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            ViewData["USERID"] = userId;
            ViewData["APIBASEURL"] = baseUrl;
            ViewData["AUTHTOKEN"] = token;

            // หมวดหมู่
            Categories = await client.GetFromJsonAsync<List<ProductCategoryDto>>($"{baseUrl}api/productcategory") ?? new();

            // 1) หา location ที่ user ได้สิทธิ์
            var allowLocations = new List<UserLocationDto>();
            if (userId > 0)
            {
                allowLocations = await client.GetFromJsonAsync<List<UserLocationDto>>(
                    $"{baseUrl}api/userlocations/user/{userId}") ?? new();
            }

            // ถ้าไม่มีสิทธิ์ที่ไหนเลย => มองไม่เห็นสินค้าใด ๆ
            if (allowLocations.Count == 0)
            {
                Products = new();
                VisibleProductIds = new();
                return;
            }

            // 2) รวม product ที่อยู่ใน ProductStocks ของแต่ละ location
            var visible = new HashSet<int>();
            foreach (var loc in allowLocations)
            {
                // ใช้ endpoint ที่มีอยู่แล้ว
                var stocks = await client.GetFromJsonAsync<List<StockRow>>(
                    $"{baseUrl}api/ProductStock/location/{loc.LocationId}?userId={userId}") ?? new();

                foreach (var s in stocks)
                    visible.Add(s.ProductId);
            }
            VisibleProductIds = visible;

            // 3) โหลดสินค้า (ทั้งหมดหรือเฉพาะหมวด) แล้ว filter ด้วย VisibleProductIds
            string url = CategoryId.HasValue
                ? $"{baseUrl}api/product/ByCategory?categoryId={CategoryId.Value}"
                : $"{baseUrl}api/product";

            var all = await client.GetFromJsonAsync<List<ProductDto>>(url) ?? new();
            Products = all.Where(p => visible.Contains(p.Id)).ToList();
        }

        public IActionResult OnPostAddToCart(int productId)
        {
            var productName = Request.Form["ProductName"].ToString();
            var imageUrl = Request.Form["ImageUrl"].ToString();
            decimal.TryParse(Request.Form["Price"], out decimal price);

            var cart = GetCart();
            var existing = cart.Find(p => p.ProductId == productId);
            if (existing != null)
                existing.Quantity += 1;
            else
                cart.Add(new CartItemDto
                {
                    ProductId = productId,
                    ProductName = productName,
                    Price = price,
                    ImageUrl = imageUrl,
                    Quantity = 1
                });

            SaveCart(cart);
            TempData["ShowToast"] = $"เพิ่มสินค้า \"{productName}\" ลงตะกร้าเรียบร้อยแล้ว";
            return RedirectToPage();
        }

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

        // ==== DTOs สำหรับเรียก API ภายในหน้านี้ ====
        public class UserLocationDto
        {
            public int UserId { get; set; }
            public int LocationId { get; set; }
        }

        public class StockRow
        {
            public int ProductId { get; set; }
            public int QtyAvailable { get; set; } // ไม่ได้ใช้ filter ติดลบ/ศูนย์ในหน้าตอนนี้ แต่เผื่ออนาคต
        }
    }
}
