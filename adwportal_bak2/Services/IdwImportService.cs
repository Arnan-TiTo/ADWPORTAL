using adwportal.Dtos;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace adwportal.Services;

public class IdwImportService
{
    private readonly IHttpClientFactory _factory;
    private readonly HttpClient _http; // client ตั้งต้นจาก name "IdwApiBaseUrl"

    public IdwImportService(IHttpClientFactory factory)
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

    private static JsonSerializerOptions JsonOpts => new() { PropertyNameCaseInsensitive = true };

    private HttpClient CreateClient(string? token)
    {
        var http = _factory.CreateClient("IdwApiBaseUrl"); // reuse same named client
        if (!string.IsNullOrWhiteSpace(token))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NormalizeToken(token));
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return http;
    }

    // ===== Auth =====
    public async Task<LoginResponseDtos?> LoginAsync(string username, string password)
    {
        var payload = new { username, password };
        var res = await _http.PostAsJsonAsync("api/Auth/login", payload);
        if (!res.IsSuccessStatusCode) return null;

        var loginResponse = await res.Content.ReadFromJsonAsync<LoginResponseDtos>();
        if (loginResponse != null)
        {
            await SetSessionAsync(loginResponse);
        }
        return loginResponse;
    }

    public async Task<bool> SetSessionAsync(LoginResponseDtos Dtos)
    {
        // ถ้ามี endpoint ฝั่ง server สำหรับผูก session/token
        var res = await _http.PostAsJsonAsync("IdwToken/set-session", Dtos);
        return res.IsSuccessStatusCode;
    }

    // ===== Import (UPDATED: รองรับ shopId/miscIdPlatform/miscIdLogistic) =====
    public async Task<UploadResponseDtos> UploadImportAsync(
        string token,
        Stream fileStream,
        string fileName,
        int? shopId = null,
        int? miscIdPlatform = null,
        int? miscIdLogistic = null,
        CancellationToken ct = default)
    {
        using var http = CreateClient(token);

        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        form.Add(fileContent, "file", fileName);

        if (shopId.HasValue) form.Add(new StringContent(shopId.Value.ToString()), "shopId");

        if (miscIdPlatform.HasValue) form.Add(new StringContent(miscIdPlatform.Value.ToString()), "miscIdPlatform");
        if (miscIdLogistic.HasValue) form.Add(new StringContent(miscIdLogistic.Value.ToString()), "miscIdLogistic");

        // ตรงกับ [ApiController]/IdwController -> [HttpPost("import")]
        var resp = await http.PostAsync("api/idw/import", form, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Import failed {(int)resp.StatusCode}: {body}");

        var Dtos = JsonSerializer.Deserialize<UploadResponseDtos>(body, JsonOpts)
                  ?? throw new Exception("Invalid response: cannot parse UploadResponseDtos.");
        return Dtos;
    }

    // ===== Read imported detail =====
    public async Task<IdwImportDtos?> GetImportAsync(string token, long id)
    {
        using var http = CreateClient(token);
        var res = await http.GetAsync($"api/TblImport/{id}");
        var json = await res.Content.ReadAsStringAsync();

        Console.WriteLine("DEBUG GetImportAsync response:");
        Console.WriteLine(json);

        if (!res.IsSuccessStatusCode) return null;
        return JsonSerializer.Deserialize<IdwImportDtos>(json, JsonOpts);
    }

    /// <summary>
    /// ค้นหารายการ Import เพื่อใช้เลือก Batch No (รองรับหลายรูปแบบ response)
    /// </summary>
    public async Task<PagedResult<IdwImportDtos>> SearchImportsAsync(
        string token,
        string? batchContains,
        int page = 1,
        int size = 50,
        CancellationToken ct = default)
    {
        using var http = CreateClient(token);

        // พยายามเรียก endpoint ที่มักพบ: GET api/TblImport (อาจรองรับ query)
        async Task<(bool ok, string json)> TryGet(string url)
        {
            var res = await http.GetAsync(url, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            return (res.IsSuccessStatusCode, body);
        }

        var qs = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(batchContains)) qs["batchNo"] = batchContains;
        if (page > 0) qs["page"] = page.ToString(CultureInfo.InvariantCulture);
        if (size > 0) qs["size"] = size.ToString(CultureInfo.InvariantCulture);

        string url1 = qs.Count > 0
            ? Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("api/TblImport", qs)
            : "api/TblImport";

        // fallback urls (บาง backend จะใช้ /list หรือ /search)
        string url2 = qs.Count > 0
            ? Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("api/TblImport/list", qs)
            : "api/TblImport/list";
        string url3 = qs.Count > 0
            ? Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("api/TblImport/search", qs)
            : "api/TblImport/search";

        var tries = new[] { url1, url2, url3 };
        string lastJson = "";

        foreach (var u in tries)
        {
            var (ok, json) = await TryGet(u);
            lastJson = json;
            if (!ok) continue;

            // 1) PagedResult<IdwImportDtos>
            try
            {
                var paged = JsonSerializer.Deserialize<PagedResult<IdwImportDtos>>(json, JsonOpts);
                if (paged?.Items is not null)
                    return paged;
            }
            catch { /* ignore */ }

            // 2) List<IdwImportDtos>
            try
            {
                var list = JsonSerializer.Deserialize<List<IdwImportDtos>>(json, JsonOpts);
                if (list is not null)
                {
                    return new PagedResult<IdwImportDtos>
                    {
                        Page = page,
                        Size = size,
                        TotalItems = list.Count,
                        Items = list
                    };
                }
            }
            catch { /* ignore */ }
        }

        throw new Exception($"SearchImports failed: {lastJson}");
    }

    // ===== Search (ยังรองรับหลายทรงของ API) =====
    public async Task<PagedResult<IdwOrderDtos>> SearchAsync(
        string? token,
        string? batchNo,
        string? orderNo,
        string? sku,
        int page,
        int size,
        CancellationToken ct = default)
    {
        using var http = CreateClient(token);

        var basePath = $"api/TblImport/batch/{Uri.EscapeDataString(batchNo ?? string.Empty)}";

        var qs = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(orderNo)) qs["orderNo"] = orderNo;
        if (!string.IsNullOrWhiteSpace(sku)) qs["sku"] = sku;
        if (page > 0) qs["page"] = page.ToString(CultureInfo.InvariantCulture);
        if (size > 0) qs["size"] = size.ToString(CultureInfo.InvariantCulture);

        var url = qs.Count > 0 ? Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(basePath, qs) : basePath;

        var res = await http.GetAsync(url, ct);
        var json = await res.Content.ReadAsStringAsync(ct);

        Console.WriteLine($"[IDW Search] GET {url} -> {(int)res.StatusCode}");
        Console.WriteLine(json);

        if (!res.IsSuccessStatusCode)
            throw new Exception($"Search failed {(int)res.StatusCode}: {json}");

        // --- ทรงที่ 1: ตัวเดียวแบบ IdwImportDtos (มี Rows) ---
        try
        {
            var one = JsonSerializer.Deserialize<IdwImportDtos>(json, JsonOpts);
            if (one?.Rows != null && one.Rows.Count > 0)
                return MapRowsToPaged(one.Rows, page, size, batchNo);
        }
        catch { /* ignore */ }

        // --- ทรงที่ 2: PagedResult<IdwImportRowDtos> ---
        try
        {
            var paged = JsonSerializer.Deserialize<PagedResult<IdwImportRowDtos>>(json, JsonOpts);
            if (paged?.Items != null)
            {
                var items = paged.Items.Select(ToOrderDtos(batchNo)).ToList();
                return new PagedResult<IdwOrderDtos>
                {
                    Page = paged.Page,
                    Size = paged.Size,
                    TotalItems = paged.TotalItems,
                    Items = items
                };
            }
        }
        catch { /* ignore */ }

        // --- ทรงที่ 3: List<IdwImportRowDtos> ---
        try
        {
            var list = JsonSerializer.Deserialize<List<IdwImportRowDtos>>(json, JsonOpts) ?? new();
            return MapRowsToPaged(list, page, size, batchNo);
        }
        catch { }

        return new PagedResult<IdwOrderDtos> { Page = page, Size = size, Items = new() };

        // === local functions ===
        static PagedResult<IdwOrderDtos> MapRowsToPaged(IEnumerable<IdwImportRowDtos> rows, int page, int size, string? batchNo)
        {
            var items = rows.Select(ToOrderDtos(batchNo)).ToList();
            return new PagedResult<IdwOrderDtos>
            {
                Page = page,
                Size = size,
                TotalItems = items.Count,
                Items = items
            };
        }

        static Func<IdwImportRowDtos, IdwOrderDtos> ToOrderDtos(string? batchNo) => r => new IdwOrderDtos
        {
            Id = r.Id,
            BatchNo = batchNo,
            OrderNo = r.OrderNo,
            Sku = r.ItemSdk,
            PickupDate = r.PickupDate,
            ShipByDate = r.ShipByDate,
            SenderName = r.SenderName,
            SenderAddress = r.SenderAddress,
            ReceiverName = r.ReceiverName,
            ReceiveAddress = r.ReceiverAddress,
            ProductName = r.ItemName,
            Variant = r.ItemVariant,
            Cod = r.IsCod.HasValue ? (r.IsCod.Value ? "Y" : "N") : null,
            Qty = r.Qty
        };
    }

    public async Task<IdwImportRowDtos?> GetImportRowAsync(string token, long id)
    {
        using var http = CreateClient(token);
        var res = await http.GetAsync($"api/TblImportRow/{id}");
        var json = await res.Content.ReadAsStringAsync();

        Console.WriteLine("DEBUG GetImportRowAsync response:");
        Console.WriteLine(json);

        if (!res.IsSuccessStatusCode) return null;

        return JsonSerializer.Deserialize<IdwImportRowDtos>(json, JsonOpts);
    }

    public async Task UpdateImportRowAsync(string token, IdwImportRowDtos row)
    {
        using var http = CreateClient(token);
        var res = await http.PutAsJsonAsync($"api/TblImportRow/{row.Id}", row);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteImportAsync(string token, long id)
    {
        using var http = CreateClient(token);
        var res = await http.DeleteAsync($"api/TblImport/{id}");
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteImportByBatchAsync(string token, string batchNo)
    {
        using var http = CreateClient(token);
        var res = await http.DeleteAsync($"api/TblImport/batch/{Uri.EscapeDataString(batchNo)}");
        res.EnsureSuccessStatusCode();
    }

    // ---- NEW: dropdown sources ----
    public async Task<List<CompanyItemDtos>> GetCompaniesAsync(string token, CancellationToken ct = default)
    {
        using var http = CreateClient(token);
        var res = await http.GetFromJsonAsync<List<CompanyItemDtos>>("api/companys", ct);
        return res ?? new List<CompanyItemDtos>();
    }

    public async Task<List<MiscItemDtos>> GetPlatformsAsync(string token, CancellationToken ct = default)
    {
        using var http = CreateClient(token);
        var res = await http.GetFromJsonAsync<List<MiscItemDtos>>("api/misc/platforms", ct);
        return res ?? new List<MiscItemDtos>();
    }

    public async Task<List<MiscItemDtos>> GetLogisticsAsync(string token, CancellationToken ct = default)
    {
        using var http = CreateClient(token);
        var res = await http.GetFromJsonAsync<List<MiscItemDtos>>("api/misc/logistics", ct);
        return res ?? new List<MiscItemDtos>();
    }

    public async Task<List<MdwPartnerDtos>> GetPartnersAsync(string token, int? partnerExternalId = null, CancellationToken ct = default)
    {
        using var http = CreateClient(token);
        var url = "api/companys/partners";
        if (partnerExternalId is > 0) url += $"?partnerId={partnerExternalId.Value}";
        var res = await http.GetFromJsonAsync<List<MdwPartnerDtos>>(url, ct);
        return res ?? new();
    }

}
