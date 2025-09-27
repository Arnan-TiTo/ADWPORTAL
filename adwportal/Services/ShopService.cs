using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;                 // สำหรับ HttpUtility.UrlEncode (หรือใช้ Uri.EscapeDataString ก็ได้)
using adwportal.Dtos;

namespace adwportal.Services;

public class ShopService
{
    private readonly IHttpClientFactory _factory;
    public ShopService(IHttpClientFactory factory) => _factory = factory;

    private static string NormalizeToken(string? t)
    {
        if (string.IsNullOrWhiteSpace(t)) return "";
        t = t.Trim().Trim('"');
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) t = t[7..].Trim();
        return t;
    }

    private HttpClient Create(string token)
    {
        var http = _factory.CreateClient("IdwApiBaseUrl");
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", NormalizeToken(token));
        return http;
    }

    /// <summary>
    /// ดึงรายชื่อร้าน (รองรับกรองตาม partnerId และ/หรือ name)
    /// </summary>
    public async Task<List<ShopDtos>> GetShopsAsync(
        string token,
        int? partnerId = null,
        string? name = null,
        CancellationToken ct = default)
    {
        using var http = Create(token);

        var qs = new List<string>();
        if (partnerId is > 0) qs.Add($"partnerId={partnerId.Value}");
        if (!string.IsNullOrWhiteSpace(name)) qs.Add($"name={Uri.EscapeDataString(name)}");

        var url = "api/shops" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        var res = await http.GetAsync(url, ct);

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"GetShops failed {(int)res.StatusCode}: {body}");
        }

        return await res.Content.ReadFromJsonAsync<List<ShopDtos>>(cancellationToken: ct) ?? new();
    }

    public async Task<ShopDtos?> GetShopAsync(string token, int id, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.GetAsync($"api/shops/{id}", ct);

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"GetShop failed {(int)res.StatusCode}: {body}");
        }

        return await res.Content.ReadFromJsonAsync<ShopDtos>(cancellationToken: ct);
    }

    public async Task<ShopDtos> CreateShopAsync(string token, ShopUpsertDtos dto, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync("api/shops", dto, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ShopDtos>(cancellationToken: ct))!;
    }

    public async Task<ShopDtos> UpdateShopAsync(string token, int id, ShopUpsertDtos dto, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PutAsJsonAsync($"api/shops/{id}", dto, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ShopDtos>(cancellationToken: ct))!;
    }

    public async Task DeleteShopAsync(string token, int id, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.DeleteAsync($"api/shops/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }
}
