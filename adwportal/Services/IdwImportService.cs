using adwportal.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace adwportal.Services;

public class IdwImportService
{
    private readonly HttpClient _http;

    public IdwImportService(IHttpClientFactory factory)
    {
        // ต้องตั้ง DI ไว้ก่อนหน้า: builder.Services.AddHttpClient("IdwApiBaseUrl", ...)
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

    // ===== Auth =====
    public async Task<LoginResponseDto?> LoginAsync(string username, string password)
    {
        var payload = new { username, password };
        var res = await _http.PostAsJsonAsync("api/Auth/login", payload);
        if (!res.IsSuccessStatusCode) return null;

        var loginResponse = await res.Content.ReadFromJsonAsync<LoginResponseDto>();
        if (loginResponse != null)
        {
            await SetSessionAsync(loginResponse);
        }
        return loginResponse;
    }

    public async Task<bool> SetSessionAsync(LoginResponseDto dto)
    {
        // สมมติฝั่ง API มี endpoint เก็บ session/token ฝั่ง server
        var res = await _http.PostAsJsonAsync("IdwToken/set-session", dto);
        return res.IsSuccessStatusCode;
    }

    // ===== Import (UPDATED: return UploadResponseDto) =====
    public async Task<UploadResponseDto> UploadImportAsync(string token, Stream fileStream, string fileName)
    {
        token = NormalizeToken(token);

        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream"); // รองรับทุกไฟล์
        form.Add(fileContent, "file", fileName);

        using var req = new HttpRequestMessage(HttpMethod.Post, "api/idw/import") { Content = form };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var res = await _http.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
        {
            var waHeader = res.Headers.WwwAuthenticate.FirstOrDefault();
            var wa = waHeader?.ToString();

            throw new Exception($"Import failed {(int)res.StatusCode} {res.ReasonPhrase}. " +
                                (string.IsNullOrEmpty(wa) ? "" : $"WWW-Authenticate: {wa}. ") +
                                $"Body: {body}");
        }

        var dto = JsonSerializer.Deserialize<UploadResponseDto>(body, JsonOpts)
                  ?? throw new Exception("Invalid response from server: cannot parse UploadResponseDto.");
        return dto;
    }

    // ===== Read imported detail =====
    public async Task<IdwImportDto?> GetImportAsync(string token, long id)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"api/TblImport/{id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NormalizeToken(token));

        var res = await _http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        Console.WriteLine("DEBUG GetImportAsync response:");
        Console.WriteLine(json);

        if (!res.IsSuccessStatusCode) return null;

        return JsonSerializer.Deserialize<IdwImportDto>(json, JsonOpts);
    }

    // ===== Search (ยังรองรับหลายทรงของ API ตามเดิม) =====
    // GET /api/TblImport/batch/{batchNo}?page=&size=&orderNo=&sku=
    public async Task<PagedResult<IdwOrderDto>> SearchAsync(
        string? token,
        string? batchNo,
        string? orderNo,
        string? sku,
        int page,
        int size,
        CancellationToken ct = default)
    {
        var basePath = $"api/TblImport/batch/{Uri.EscapeDataString(batchNo ?? string.Empty)}";

        var qs = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(orderNo)) qs["orderNo"] = orderNo;
        if (!string.IsNullOrWhiteSpace(sku)) qs["sku"] = sku;
        if (page > 0) qs["page"] = page.ToString(CultureInfo.InvariantCulture);
        if (size > 0) qs["size"] = size.ToString(CultureInfo.InvariantCulture);

        var url = qs.Count > 0 ? QueryHelpers.AddQueryString(basePath, qs) : basePath;

        var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrWhiteSpace(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NormalizeToken(token));

        var res = await _http.SendAsync(req, ct);
        var json = await res.Content.ReadAsStringAsync(ct);

        Console.WriteLine($"[IDW Search] GET {url} -> {(int)res.StatusCode}");
        Console.WriteLine(json);

        if (!res.IsSuccessStatusCode)
            throw new Exception($"Search failed {(int)res.StatusCode}: {json}");

        // --- ทรงที่ 1: ตัวเดียวแบบ IdwImportDto (มี Rows) ---
        try
        {
            var one = JsonSerializer.Deserialize<IdwImportDto>(json, JsonOpts);
            if (one?.Rows != null && one.Rows.Count > 0)
                return MapRowsToPaged(one.Rows, page, size, batchNo);
        }
        catch { /* ignore and try next shape */ }

        // --- ทรงที่ 2: PagedResult<IdwImportRowDto> ---
        try
        {
            var paged = JsonSerializer.Deserialize<PagedResult<IdwImportRowDto>>(json, JsonOpts);
            if (paged?.Items != null)
            {
                var items = paged.Items.Select(ToOrderDto(batchNo)).ToList();
                return new PagedResult<IdwOrderDto>
                {
                    Page = paged.Page,
                    Size = paged.Size,
                    TotalItems = paged.TotalItems,
                    Items = items
                };
            }
        }
        catch { /* ignore and try next shape */ }

        // --- ทรงที่ 3: List<IdwImportRowDto> ---
        try
        {
            var list = JsonSerializer.Deserialize<List<IdwImportRowDto>>(json, JsonOpts) ?? new();
            return MapRowsToPaged(list, page, size, batchNo);
        }
        catch { /* final fallback below */ }

        // ไม่เข้าได้สักทรง → คืนว่าง
        return new PagedResult<IdwOrderDto> { Page = page, Size = size, Items = new() };

        // === local functions ===
        static PagedResult<IdwOrderDto> MapRowsToPaged(IEnumerable<IdwImportRowDto> rows, int page, int size, string? batchNo)
        {
            var items = rows.Select(ToOrderDto(batchNo)).ToList();
            return new PagedResult<IdwOrderDto>
            {
                Page = page,
                Size = size,
                TotalItems = items.Count,
                Items = items
            };
        }

        static Func<IdwImportRowDto, IdwOrderDto> ToOrderDto(string? batchNo) => r => new IdwOrderDto
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

    public async Task<IdwImportRowDto?> GetImportRowAsync(string token, long id)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"api/TblImportRow/{id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NormalizeToken(token));

        var res = await _http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        Console.WriteLine("DEBUG GetImportRowAsync response:");
        Console.WriteLine(json);

        if (!res.IsSuccessStatusCode) return null;

        return JsonSerializer.Deserialize<IdwImportRowDto>(json, JsonOpts);
    }

    public async Task UpdateImportRowAsync(string token, IdwImportRowDto row)
    {
        var req = new HttpRequestMessage(HttpMethod.Put, $"api/TblImportRow/{row.Id}")
        {
            Content = JsonContent.Create(row)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NormalizeToken(token));

        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteImportAsync(string token, long id)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/TblImport/{id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NormalizeToken(token));

        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteImportByBatchAsync(string token, string batchNo)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/TblImport/batch/{Uri.EscapeDataString(batchNo)}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NormalizeToken(token));
        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
    }
}
