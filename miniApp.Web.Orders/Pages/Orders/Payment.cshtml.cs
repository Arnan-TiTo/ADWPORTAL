using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.WebOrders.Models;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace miniApp.WebOrders.Pages.Orders
{
    public class PaymentModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        [BindProperty] public List<CartItemDto> Cart { get; set; } = new();
        [BindProperty] public string PaymentMethod { get; set; } = "transfer";
        [BindProperty] public IFormFile? Slip { get; set; }

        public int LocationId => Cart.FirstOrDefault()?.LocationId ?? 0;
        public int TotalItems => Cart.Sum(p => p.Quantity);
        public decimal Subtotal => Cart.Sum(p => p.Price * p.Quantity);
        public decimal DiscountTotal => Cart.Sum(p => p.Discount);
        public decimal Total => Subtotal - DiscountTotal;


        public PaymentModel(IHttpClientFactory httpClientFactory, IConfiguration config)
            => (_httpClientFactory, _config) = (httpClientFactory, config);

        public void OnGet()
        {
            Cart = GetCart();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Cart = GetCart();

            var customerJson = HttpContext.Session.GetString("ORDER_CUSTOMER");
            if (string.IsNullOrEmpty(customerJson))
                return RedirectToPage("/Orders/Customer");

            var customer = JsonSerializer.Deserialize<CustomerInfoModel>(customerJson);
            if (customer == null)
                return RedirectToPage("/Orders/Customer");

            string? slipImageFileName = null;
            if (Slip != null && Slip.Length > 0)
            {
                var root = _config["ImageRootPath"];
                if (string.IsNullOrWhiteSpace(root))
                {
                    ModelState.AddModelError(string.Empty, "ImageRootPath is not configured.");
                    return Page();
                }

                var saveDir = Path.Combine(root, "slips");
                Directory.CreateDirectory(saveDir);

                var safeName = Path.GetFileName(Slip.FileName);
                var fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{safeName}";
                var savePath = Path.Combine(saveDir, fileName);
                using var fs = new FileStream(savePath, FileMode.Create);
                await Slip.CopyToAsync(fs);

                slipImageFileName = fileName;
            }

            var userId = HttpContext.Session.GetInt32("USERID") ?? 0;
            if (userId == 0)
            {
                ModelState.AddModelError(string.Empty, "กรุณาเข้าสู่ระบบก่อนทำรายการ");
                return Page();
            }

            // --- DTO ส่งเข้า API ---
            var orderDto = new OrderCreateDto
            {
                CustomerName = customer.CustomerName ?? "",
                Gender = customer.Gender ?? "",
                BirthDate = customer.BirthDate,
                Occupation = customer.Occupation ?? "",
                Nationality = customer.Nationality ?? "",
                CustomerPhone = customer.CustomerPhone ?? "",
                CustomerEmail = customer.CustomerEmail ?? "",
                AddressLine = customer.AddressLine ?? "",
                SubDistrict = customer.SubDistrict ?? "",
                District = customer.District ?? "",
                Province = customer.Province ?? "",
                ZipCode = customer.ZipCode ?? "",
                Social = customer.Social,  //JSON string
                MayIAsk = customer.MayIAsk,
                PaymentMethod = PaymentMethod ?? "",
                SlipImage = slipImageFileName,
                CreatedByUserId = userId,
                Items = Cart.Select(c => new OrderItemDto
                {  
                    LocationId = c.LocationId,
                    ProductId = c.ProductId,
                    ProductName = c.ProductName,
                    Quantity = c.Quantity,
                    UnitPrice = c.Price,
                    Discount = c.Discount
                }).ToList()
            };

            var baseUrl = _config["APIBASEURL"]?.TrimEnd('/') + "/";
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                ModelState.AddModelError(string.Empty, "APIBASEURL is not configured.");
                return Page();
            }

            var token = _config["AUTHTOKEN"] ?? "";
            var client = _httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await client.PostAsJsonAsync($"{baseUrl}api/Order", orderDto);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"บันทึกออเดอร์ไม่สำเร็จ: {err}");
                return Page();
            }

            HttpContext.Session.Remove("Cart");
            HttpContext.Session.Remove("ORDER_CUSTOMER");
            TempData["ShowToast"] = "บันทึกข้อมูลเรียบร้อยแล้ว";
            TempData["NextUrl"] = Url.Page("/ProductSearch");
            TempData["DelayMs"] = 2000; 

            return Page();
        }

        private List<CartItemDto> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson)) return new();
            try { return JsonSerializer.Deserialize<List<CartItemDto>>(cartJson) ?? new(); }
            catch { return new(); }
        }
    }
}
