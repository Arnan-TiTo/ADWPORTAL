using System.Net.Http.Headers;
using System.Net.Http.Json;
using adwportal.Dtos;

namespace adwportal.Services;

public class PartnerService
{
    private readonly IHttpClientFactory _factory;
    public PartnerService(IHttpClientFactory factory) => _factory = factory;

    private HttpClient Create(string token)
    {
        var http = _factory.CreateClient("IdwApiBaseUrl");
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                token?.Trim().Trim('"').Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase));
        return http;
    }

    public async Task<List<PartnerDtos>> GetPartnersAsync(string token, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<List<PartnerDtos>>("api/partners", ct) ?? new();
    }

    public async Task<PartnerDtos?> GetPartnerAsync(string token, int id, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<PartnerDtos>($"api/partners/{id}", ct);
    }

    public async Task<PartnerDtos> CreatePartnerAsync(string token, PartnerUpsertDtos dto, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync("api/partners", dto, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<PartnerDtos>(cancellationToken: ct))!;
    }

    public async Task<PartnerDtos> UpdatePartnerAsync(string token, int id, PartnerUpsertDtos dto, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PutAsJsonAsync($"api/partners/{id}", dto, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<PartnerDtos>(cancellationToken: ct))!;
    }

    public async Task DeletePartnerAsync(string token, int id, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.DeleteAsync($"api/partners/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }
}
