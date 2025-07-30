using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.WebOrders.Models;
using System.Text.Json;

namespace miniApp.WebOrders.Pages.Orders
{
    public class CustomerModel : PageModel
    {
        [BindProperty]
        public CustomerInfoModel Customer { get; set; } = new();

        public void OnGet()
        {
            var json = HttpContext.Session.GetString("ORDER_CUSTOMER");
            if (!string.IsNullOrEmpty(json))
            {
                Customer = JsonSerializer.Deserialize<CustomerInfoModel>(json) ?? new();
            }
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var json = JsonSerializer.Serialize(Customer);
            HttpContext.Session.SetString("ORDER_CUSTOMER", json);

            return RedirectToPage("Payment");
        }
    }

    public class CustomerInfoModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string Occupation { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string SubDistrict { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public bool MayIAsk { get; set; } = false;
    }
}
