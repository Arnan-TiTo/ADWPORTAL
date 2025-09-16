using Microsoft.AspNetCore.WebUtilities;
using miniApp.AdminPortal.Models;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace miniApp.AdminPortal.Services;

public class IdwImportService
{
    private readonly HttpClient _http;

    public IdwImportService(IHttpClientFactory factory)
    {
        // ชื่อ client ต้องไปกำหนดไว้ใน DI: builder.Services.AddHttpClient("IdwApiBaseUrl", ...)
        _http = factory.CreateClient("IdwApiBaseUrl");
    }

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

    // ===== Import =====
    public async Task<string> UploadImportAsync(string token, Stream fileStream, string fileName)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, "api/idw/import")
        {
            Content = form
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await _http.SendAsync(request);
        var body = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
            throw new Exception($"Import failed ({res.StatusCode}): {body}");

        var uploadResponse = JsonSerializer.Deserialize<UploadResponseDto>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (uploadResponse == null)
            throw new Exception("Invalid response from server: cannot parse UploadResponseDto.");

        return uploadResponse.importId.ToString();
    }

    public async Task<IdwImportDto?> GetImportAsync(string token, long id)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"api/TblImport/{id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await _http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        Console.WriteLine("DEBUG GetImportAsync response:");
        Console.WriteLine(json);

        if (!res.IsSuccessStatusCode) return null;

        return JsonSerializer.Deserialize<IdwImportDto>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // GET /api/idw/TblImport/batch=&orderNo=&sku=&page=1&size=20
    public async Task<PagedResult<IdwOrderDto>> SearchAsync(
     string? token,
     string? batchNo,
     string? orderNo,
     string? sku,
     int page,
     int size,
     CancellationToken ct = default)
    {
        // ตามที่ทดสอบใน Postman: /api/TblImport/batch/{batchNo}?page=&size=&orderNo=&sku=
        var basePath = $"api/TblImport/batch/{Uri.EscapeDataString(batchNo ?? string.Empty)}";

        var qs = new Dictionary<string, string?>();

        if (!string.IsNullOrWhiteSpace(orderNo)) qs["orderNo"] = orderNo;
        if (!string.IsNullOrWhiteSpace(sku)) qs["sku"] = sku;
        if (page > 0) qs["page"] = page.ToString(CultureInfo.InvariantCulture);
        if (size > 0) qs["size"] = size.ToString(CultureInfo.InvariantCulture);

        var url = qs.Count > 0
            ? QueryHelpers.AddQueryString(basePath, qs)
            : basePath;

        var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrWhiteSpace(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await _http.SendAsync(req, ct);
        var json = await res.Content.ReadAsStringAsync(ct);

        Console.WriteLine($"[IDW Search] GET {url} -> {(int)res.StatusCode}");
        Console.WriteLine(json);

        if (!res.IsSuccessStatusCode)
            throw new Exception($"Search failed {(int)res.StatusCode}: {json}");

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // --- ทรงที่ 1: ตัวเดียวแบบ IdwImportDto (มี Rows) ---
        try
        {
            var one = JsonSerializer.Deserialize<IdwImportDto>(json, opts);
            if (one?.Rows != null && one.Rows.Count > 0)
                return MapRowsToPaged(one.Rows, page, size, batchNo);
        }
        catch { }

        // --- ทรงที่ 2: PagedResult<IdwImportRowDto> ---
        try
        {
            var paged = JsonSerializer.Deserialize<PagedResult<IdwImportRowDto>>(json, opts);
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
        catch { }

        // --- ทรงที่ 3: List<IdwImportRowDto> ---
        try
        {
            var list = JsonSerializer.Deserialize<List<IdwImportRowDto>>(json, opts) ?? new();
            return MapRowsToPaged(list, page, size, batchNo);
        }
        catch { }

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
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await _http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        Console.WriteLine("DEBUG GetImportRowAsync response:");
        Console.WriteLine(json);

        if (!res.IsSuccessStatusCode) return null;

        return JsonSerializer.Deserialize<IdwImportRowDto>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task UpdateImportRowAsync(string token, IdwImportRowDto row)
    {
        var req = new HttpRequestMessage(HttpMethod.Put, $"api/TblImportRow/{row.Id}")
        {
            Content = JsonContent.Create(row)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteImportAsync(string token, int id)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/TblImport/{id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteImportByBatchAsync(string token, string batchNo)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/TblImport/batch/{Uri.EscapeDataString(batchNo)}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
    }

}

