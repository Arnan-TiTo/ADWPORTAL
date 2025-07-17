using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.WebOrders.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

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

        public async Task OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _config["APIBASEURL"] ?? "http://localhost:5252";
            var token = _config["AUTHTOKEN"] ?? "";

            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var userId = HttpContext.Session.GetInt32("USERID") ?? 0;

            ViewData["USERID"] = userId;
            ViewData["APIBASEURL"] = baseUrl;
            ViewData["AUTHTOKEN"] = token;

            Categories = await client.GetFromJsonAsync<List<ProductCategoryDto>>($"{baseUrl}/api/productcategory") ?? new();

            string url = CategoryId.HasValue
                ? $"{baseUrl}/api/product/ByCategory?categoryId={CategoryId.Value}"
                : $"{baseUrl}/api/product";

            Products = await client.GetFromJsonAsync<List<ProductDto>>(url) ?? new();
        }

        public IActionResult OnPostAddToCart(int productId)
        {
            var productName = Request.Form["ProductName"].ToString();
            var imageUrl = Request.Form["ImageUrl"].ToString();

            decimal.TryParse(Request.Form["Price"], out decimal price);

            Console.WriteLine($"ADD TO CART: {productId}, {productName}, {price}, {imageUrl}");

            var cart = GetCart();
            var existing = cart.Find(p => p.ProductId == productId);
            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                cart.Add(new CartItemDto
                {
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
    }
}
