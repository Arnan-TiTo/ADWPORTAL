using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace miniApp.Web.Pages
{
    public class ProfileModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _clientFactory;

        public ProfileModel(IConfiguration config, IHttpClientFactory clientFactory)
        {
            _config = config;
            _clientFactory = clientFactory;
        }

        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string RoleDescription { get; set; } = "";

        public async Task OnGetAsync()
        {
            var USERID = HttpContext.Session.GetInt32("USERID") ?? 0;
            var APIBASEURL = _config["APIBASEURL"] ?? "";
            var AUTHTOKEN = _config["AUTHTOKEN"] ?? Environment.GetEnvironmentVariable("AuthToken") ?? "";


            var client = _clientFactory.CreateClient();
            client.BaseAddress = new Uri(APIBASEURL);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AUTHTOKEN);

            var res = await client.GetAsync($"api/Users/profilebyid?userid={USERID}");
            if (!res.IsSuccessStatusCode) return;

            var userJson = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(userJson);
            var root = doc.RootElement;

            Username = root.GetProperty("username").GetString() ?? "";
            Email = root.GetProperty("email").GetString() ?? "";
            Phone = root.GetProperty("phone").GetString() ?? "";
            RoleDescription = root.GetProperty("role").GetString() ?? "";
        }
    }
}
