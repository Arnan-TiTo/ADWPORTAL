using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.WebOrders.Models;
using System.Text.Json;

namespace miniApp.WebOrders.Pages.Orders
{
    public class SummaryModel : PageModel
    {
        [BindProperty]
        public List<CartItemDto> Cart { get; set; } = new();

        public int TotalItems => Cart.Sum(p => p.Quantity);
        public decimal Subtotal => Cart.Sum(p => p.Price * p.Quantity);
        public decimal DiscountTotal => Cart.Sum(p => p.Discount);
        public decimal Total => Subtotal - DiscountTotal;

        public void OnGet()
        {
            Cart = GetCart();
        }

        public IActionResult OnPostIncrease(int productId)
        {
            Cart = GetCart();
            var item = Cart.FirstOrDefault(p => p.ProductId == productId);
            if (item != null) item.Quantity++;
            SaveCart(Cart);
            return RedirectToPage();
        }

        public IActionResult OnPostDecrease(int productId)
        {
            Cart = GetCart();
            var item = Cart.FirstOrDefault(p => p.ProductId == productId);
            if (item != null && item.Quantity > 1) item.Quantity--;
            SaveCart(Cart);
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
    }
}
