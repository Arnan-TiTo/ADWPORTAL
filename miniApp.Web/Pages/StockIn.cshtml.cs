using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace miniApp.Web.Pages
{
    public class StockInModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string APIBASEURL;
        private readonly string AUTHTOKEN;

        public StockInModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            APIBASEURL = _configuration["APIBASEURL"] ?? "";
            AUTHTOKEN = _configuration["AUTHTOKEN"] ?? Environment.GetEnvironmentVariable("AuthToken") ?? "";
        }

        public List<LocationItem> LocationDropdown { get; set; } = new();
        public List<BrandItem> BrandDropdown { get; set; } = new();
        public List<CategoryItem> CategoryDropdown { get; set; } = new();

        public string CurrentUserFullname { get; set; } = "";
        public int CurrentUserId { get; set; }

        public async Task OnGetAsync()
        {
            ViewData["APIBASEURL"] = APIBASEURL;
            ViewData["AUTHTOKEN"] = AUTHTOKEN;

            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", AUTHTOKEN);

            var locationResponse = await http.GetFromJsonAsync<List<LocationItem>>($"{APIBASEURL}api/locations/dropdown");
            if (locationResponse != null)
                LocationDropdown = locationResponse;

            var brandResponse = await http.GetFromJsonAsync<List<BrandItem>>($"{APIBASEURL}api/ProductBrand");
            if (brandResponse != null)
                BrandDropdown = brandResponse;

            var categoryResponse = await http.GetFromJsonAsync<List<CategoryItem>>($"{APIBASEURL}api/ProductCategory");
            if (categoryResponse != null)
                CategoryDropdown = categoryResponse;

            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            CurrentUserFullname = User.FindFirst("FULLNAME")?.Value ?? "";
            CurrentUserId = Convert.ToInt32( User.FindFirst("USERID")?.Value);

        }

        public class LocationItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }
        public class BrandItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }
        public class CategoryItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }
        public class UserResponse
        {
            public int Id { get; set; }
            public string Fullname { get; set; } = "";
        }
    }
}
