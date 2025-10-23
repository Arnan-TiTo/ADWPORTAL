using adwportal.Dtos;
using System.Net.Http.Headers;

public class CompanyService
{
    private readonly IHttpClientFactory _factory;
    public CompanyService(IHttpClientFactory factory) => _factory = factory;

    private HttpClient Create(string token)
    {
        var http = _factory.CreateClient("IdwApiBaseUrl");
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token?.Trim().Trim('"').Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase));
        return http;
    }

    public async Task<List<MdwPartnerDtos>> GetPartnersAsync(string token, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<List<MdwPartnerDtos>>("api/companys/partners", ct) ?? new();
    }

    public async Task<MdwPartnerDtos?> GetPartnerByIdAsync(
        string token, int partnerId, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<MdwPartnerDtos>($"api/partners/{partnerId}", ct);
    }

    public async Task<List<MdwShopDtos>> GetShopsAsync(string token, int partnerId, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<List<MdwShopDtos>>($"api/shops?partnerId={partnerId}", ct) ?? new();
    }

    public async Task<List<CompanyListItemDtos>> GetCompaniesAsync(string token, string? name = null, int? partnerId = null, CancellationToken ct = default)
    {
        using var http = Create(token);
        var url = "api/companys/list";
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(name)) qs.Add($"name={Uri.EscapeDataString(name)}");
        if (partnerId.HasValue) qs.Add($"partnerId={partnerId.Value}");
        if (qs.Count > 0) url += "?" + string.Join("&", qs);
        return await http.GetFromJsonAsync<List<CompanyListItemDtos>>(url, ct) ?? new();
    }

    public async Task<CompanyListItemDtos?> GetCompanyAsync(string token, int id, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<CompanyListItemDtos>($"api/companys/{id}", ct);
    }

    public async Task<CompanyListItemDtos> CreateCompanyAsync(string token, CompanyUpsertDtos dto, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync("api/companys", dto, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CompanyListItemDtos>(cancellationToken: ct))!;
    }

    public async Task<CompanyListItemDtos> UpdateCompanyAsync(string token, int id, CompanyUpsertDtos dto, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PutAsJsonAsync($"api/companys/{id}", dto, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CompanyListItemDtos>(cancellationToken: ct))!;
    }

    public async Task DeleteCompanyAsync(string token, int id, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.DeleteAsync($"api/companys/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }
    public async Task<List<CompanyDtos>?> GetCompaniesDropdownAsync(string token, CancellationToken ct = default)
    {
        using var http = Create(token); 
        return await http.GetFromJsonAsync<List<CompanyDtos>>("api/companys", ct) ?? new();
    }

    public async Task<CompanyListItemDtos> CreateCompanyBasicAsync(string token, CompanyUpsertBasicDtos dto, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync("api/companys/basic", dto, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CompanyListItemDtos>(cancellationToken: ct))!;
    }

    public async Task<CompanyListItemDtos> UpdateCompanyBasicAsync(string token, int id, CompanyUpsertBasicDtos dto, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PutAsJsonAsync($"api/companys/basic/{id}", dto, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CompanyListItemDtos>(cancellationToken: ct))!;
    }

    public async Task<List<MdwPartnerDtos>> GetPartnersByCompanyAsync(
        string token, int companyId, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<List<MdwPartnerDtos>>(
            $"api/companys/partners?companyId={companyId}", ct
        ) ?? new();
    }


}
