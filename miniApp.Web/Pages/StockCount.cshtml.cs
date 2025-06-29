using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace miniApp.Web.Pages
{
    public class StockCountModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string APIBASEURL;
        private readonly string AUTHTOKEN;

        public StockCountModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            APIBASEURL = _configuration["APIBASEURL"] ?? "";
            AUTHTOKEN = _configuration["AUTHTOKEN"] ?? "";
        }

        public string SearchQuery { get; set; } = "";
        public string SortOrder { get; set; } = "";
        public List<ProductItem> Products { get; set; } = new();

        public async Task OnGetAsync(string? query = null, string? sort = null)
        {
            SortOrder = sort ?? "";

            if (SortOrder == "asc")
                Products = Products.OrderBy(p => p.Name).ToList();
            else if (SortOrder == "desc")
                Products = Products.OrderByDescending(p => p.Name).ToList();

            ViewData["APIBASEURL"] = APIBASEURL;
            ViewData["AUTHTOKEN"] = AUTHTOKEN;

            SearchQuery = query ?? "";
            SortOrder = sort ?? "";

            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AUTHTOKEN);

            List<ProductItem>? result;

            if (!string.IsNullOrEmpty(SearchQuery))
            {
                var apiUrl = $"{APIBASEURL}/api/product/productsearch?query={SearchQuery}";
                result = await http.GetFromJsonAsync<List<ProductItem>>(apiUrl);
            }
            else
            {
                var apiUrl = $"{APIBASEURL}/api/product";
                result = await http.GetFromJsonAsync<List<ProductItem>>(apiUrl);
            }

            if (result != null)
                Products = result;

            if (SortOrder == "asc")
                Products = Products.OrderBy(p => p.Name).ToList();
            else if (SortOrder == "desc")
                Products = Products.OrderByDescending(p => p.Name).ToList();
        }

        public class ProductItem
        {
            public int Id { get; set; }
            public string ImageUrl { get; set; } = "";
            public string Name { get; set; } = "";
            public string? Sku { get; set; }
            public int Quantity { get; set; }
        }
    }
}
