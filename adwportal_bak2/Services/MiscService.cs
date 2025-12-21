using System.Net.Http.Headers;
using System.Text.Json;
using adwportal.Dtos;
using System.Net.Http.Json; 

namespace adwportal.Services
{
    public class MiscService
    {
        private readonly IHttpClientFactory _factory;
        private readonly HttpClient _http; // client ตั้งต้นจาก name "IdwApiBaseUrl"

        public MiscService(IHttpClientFactory factory)
        {
            _factory = factory;
            _http = factory.CreateClient("IdwApiBaseUrl");
        }

        // ===== Helpers =====
        private static string NormalizeToken(string? t)
        {
            if (string.IsNullOrWhiteSpace(t)) return "";
            t = t.Trim().Trim('"');
            if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) t = t[7..].Trim();
            return t;
        }

        private HttpClient CreateClient(string? token)
        {
            var http = _factory.CreateClient("IdwApiBaseUrl"); // reuse same named client
            if (!string.IsNullOrWhiteSpace(token))
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NormalizeToken(token));
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return http;
        }
        public async Task<List<string>> GetMiscTypesAsync(string token, CancellationToken ct = default)
        {
            using var http = CreateClient(token);
            var res = await http.GetFromJsonAsync<List<string>>("api/misc/types", ct);
            return res ?? new List<string>();
        }

        public async Task<List<MiscDtos>> GetMiscAsync(string token, string? type = null, string? name = null, CancellationToken ct = default)
        {
            using var http = CreateClient(token);
            var url = "api/misc";
            var qs = new List<string>();
            if (!string.IsNullOrWhiteSpace(type)) qs.Add($"type={Uri.EscapeDataString(type)}");
            if (!string.IsNullOrWhiteSpace(name)) qs.Add($"name={Uri.EscapeDataString(name)}");
            if (qs.Count > 0) url += "?" + string.Join("&", qs);
            var res = await http.GetFromJsonAsync<List<MiscDtos>>(url, ct);
            return res ?? new List<MiscDtos>();
        }

        public async Task<MiscDtos?> GetMiscByIdAsync(string token, int id, CancellationToken ct = default)
        {
            using var http = CreateClient(token);
            return await http.GetFromJsonAsync<MiscDtos>($"api/misc/{id}", ct);
        }

        public async Task<MiscDtos> CreateMiscAsync(string token, MiscUpsertDtos Dtos, CancellationToken ct = default)
        {
            using var http = CreateClient(token);
            var res = await http.PostAsJsonAsync("api/misc", Dtos, ct);
            res.EnsureSuccessStatusCode();
            return (await res.Content.ReadFromJsonAsync<MiscDtos>(cancellationToken: ct))!;
        }

        public async Task<MiscDtos> UpdateMiscAsync(string token, int id, MiscUpsertDtos Dtos, CancellationToken ct = default)
        {
            using var http = CreateClient(token);
            var res = await http.PutAsJsonAsync($"api/misc/{id}", Dtos, ct);
            res.EnsureSuccessStatusCode();
            return (await res.Content.ReadFromJsonAsync<MiscDtos>(cancellationToken: ct))!;
        }

        public async Task DeleteMiscAsync(string token, int id, CancellationToken ct = default)
        {
            using var http = CreateClient(token);
            var res = await http.DeleteAsync($"api/misc/{id}", ct);
            res.EnsureSuccessStatusCode();
        }
    }
}
