using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Security.Claims;

namespace miniApp.Web.Pages
{
    public class ProductModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string APIBASEURL;
        private readonly string AUTHTOKEN;

        public ProductModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            APIBASEURL = _configuration["APIBASEURL"] ?? "";
            AUTHTOKEN = _configuration["AUTHTOKEN"] ?? "";
        }

        public List<LocationItem> LocationDropdown { get; set; } = new();
        public string CurrentUserFullname { get; set; } = "";
        public int CurrentUserId { get; set; }
        public string SearchQuery { get; set; } = "";
        public string SortOrder { get; set; } = "";
        public List<ProductItem> Products { get; set; } = new();

        public async Task OnGetAsync(string? query = null, string? sort = null)
        {
            if (string.IsNullOrEmpty(query) && string.IsNullOrEmpty(sort))
                return;

            ViewData["APIBASEURL"] = _configuration["APIBASEURL"];
            ViewData["AUTHTOKEN"] = _configuration["AUTHTOKEN"];

            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AUTHTOKEN);

            // Load dropdown
            var locationResponse = await http.GetFromJsonAsync<List<LocationItem>>($"{APIBASEURL}api/locations/dropdown");
            if (locationResponse != null)
                LocationDropdown = locationResponse;

            // Load user info
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(username))
            {
                var userInfo = await http.GetFromJsonAsync<UserResponse>($"{APIBASEURL}api/users/profile?username={username}");
                if (userInfo != null)
                {
                    CurrentUserFullname = userInfo.Fullname;
                    CurrentUserId = userInfo.Id;
                }
            }

            SearchQuery = query ?? "";
            SortOrder = sort ?? "";

            var result = string.IsNullOrEmpty(query)
                ? await http.GetFromJsonAsync<List<ProductItem>>($"{APIBASEURL}api/product")
                : await http.GetFromJsonAsync<List<ProductItem>>($"{APIBASEURL}api/product/productsearch?query={query}");

            if (result != null)
                Products = result;

            if (SortOrder == "asc")
                Products = Products.OrderBy(p => p.Name).ToList();
            else if (SortOrder == "desc")
                Products = Products.OrderByDescending(p => p.Name).ToList();
        }

        public class LocationItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class UserResponse
        {
            public int Id { get; set; }
            public string Fullname { get; set; } = "";
        }

        public class ProductItem
        {
            public int Id { get; set; }
            public string ImageUrl { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Sku { get; set; }
            public string? Description { get; set; }
            public int LocationId { get; set; }
            public string LocationName { get; set; } = string.Empty;
            public int UserId { get; set; }
            public int Quantity { get; set; }
            public string UserFullname { get; set; } = string.Empty;
            public string? Note { get; set; }
            public DateTime? CreatedAt { get; set; }
        }
    }
}
