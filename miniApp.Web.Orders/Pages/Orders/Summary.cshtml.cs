using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.WebOrders.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace miniApp.WebOrders.Pages.Orders
{
    public class SummaryModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public SummaryModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        [BindProperty]
        public List<CartItemDto> Cart { get; set; } = new();

        // ใช้สำหรับ fallback ฝั่ง JS (แต่ JS จะยิงทีละ location จาก data-loc อยู่แล้ว)
        public int LocationId { get; set; }
        public int CurrentUserId { get; set; }

        public int TotalItems => Cart.Sum(p => p.Quantity);
        public decimal Subtotal => Cart.Sum(p => p.Price * p.Quantity);
        public decimal DiscountTotal => Cart.Sum(p => p.Discount);
        public decimal Total => Subtotal - DiscountTotal;

        public void OnGet()
        {
            Cart = GetCart();

            // location แรกในตะกร้า (ถ้าไม่มีค่อยไปเอาจาก session)
            LocationId = Cart.Select(c => c.LocationId).FirstOrDefault(id => id > 0);
            if (LocationId == 0) LocationId = HttpContext.Session.GetInt32("LocationId") ?? 0;

            CurrentUserId = HttpContext.Session.GetInt32("USERID") ?? 0;

            ViewData["APIBASEURL"] = (_config["APIBASEURL"] ?? "").TrimEnd('/') + "/";
            ViewData["AUTHTOKEN"] = _config["AUTHTOKEN"] ?? "";
            ViewData["USERID"] = CurrentUserId;
        }

        // กันเพิ่มเกิน Available ฝั่ง Server (ใช้ location จาก item ในตะกร้า)
        public async Task<IActionResult> OnPostIncrease(int productId)
        {
            Cart = GetCart();

            var item = Cart.FirstOrDefault(p => p.ProductId == productId);
            if (item != null)
            {
                var locId = item.LocationId > 0 ? item.LocationId : (HttpContext.Session.GetInt32("LocationId") ?? 0);
                var userId = HttpContext.Session.GetInt32("USERID") ?? 0;

                var available = await GetAvailableAsync(productId, locId, userId);
                if (available <= 0 || item.Quantity + 1 > available)
                {
                    TempData["ShowToast"] = "จำนวนสินค้าไม่พอขาย";
                }
                else
                {
                    item.Quantity++;
                    SaveCart(Cart);
                }
            }

            // ตั้งค่า ViewData ให้ JS ใช้หลัง redirect
            ViewData["APIBASEURL"] = (_config["APIBASEURL"] ?? "").TrimEnd('/') + "/";
            ViewData["AUTHTOKEN"] = _config["AUTHTOKEN"] ?? "";
            ViewData["USERID"] = HttpContext.Session.GetInt32("USERID") ?? 0;

            return RedirectToPage();
        }

        public IActionResult OnPostDecrease(int productId)
        {
            Cart = GetCart();
            var item = Cart.FirstOrDefault(p => p.ProductId == productId);
            if (item != null && item.Quantity > 1)
            {
                item.Quantity--;
                SaveCart(Cart);
            }
            return RedirectToPage();
        }

        public IActionResult OnPostRemove(int productId)
        {
            Cart = GetCart();
            Cart.RemoveAll(p => p.ProductId == productId);
            SaveCart(Cart);
            return RedirectToPage();
        }

        public IActionResult OnPostDiscount(int productId, decimal discount)
        {
            Cart = GetCart();
            var item = Cart.FirstOrDefault(p => p.ProductId == productId);
            if (item != null)
            {
                item.Discount = discount;
                SaveCart(Cart);
            }
            return RedirectToPage();
        }

        // ===== Helpers =====
        private List<CartItemDto> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson)) return new();
            try { return JsonSerializer.Deserialize<List<CartItemDto>>(cartJson) ?? new(); }
            catch { return new(); }
        }

        private void SaveCart(List<CartItemDto> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("Cart", cartJson);
        }

        private sealed class StockRow
        {
            public int ProductId { get; set; }
            public int LocationId { get; set; }
            public int QtyOnHand { get; set; }
            public int QtyReserved { get; set; }
            public int QtyDamaged { get; set; }
            public int? QtyAvailable { get; set; }
        }

        private async Task<int> GetAvailableAsync(int productId, int locationId, int userId)
        {
            if (productId <= 0 || locationId <= 0 || userId <= 0) return 0;

            var api = (_config["APIBASEURL"] ?? "").TrimEnd('/') + "/";
            var token = _config["AUTHTOKEN"] ?? "";
            var url = $"{api}api/ProductStock/location/{locationId}?userId={userId}";

            try
            {
                var client = _http.CreateClient();
                if (!string.IsNullOrWhiteSpace(token))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var rows = await client.GetFromJsonAsync<List<StockRow>>(url, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();

                var r = rows.FirstOrDefault(x => x.ProductId == productId);
                if (r == null) return 0;

                var avail = r.QtyAvailable ?? (r.QtyOnHand - r.QtyReserved - r.QtyDamaged);
                return Math.Max(0, avail);
            }
            catch
            {
                return 0;
            }
        }
    }
}
