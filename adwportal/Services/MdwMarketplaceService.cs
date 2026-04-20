using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using adwportal.Dtos;

namespace adwportal.Services
{
    public sealed class MdwMarketplaceService
    {
        private readonly IHttpClientFactory _factory;
        private readonly HttpClient _http;

        public MdwMarketplaceService(IHttpClientFactory factory)
        {
            _factory = factory;
            _http = factory.CreateClient("MdwApiBaseUrl");
        }

        // ===== Helpers =====
        private static string NormalizeToken(string? t)
        {
            if (string.IsNullOrWhiteSpace(t)) return "";
            t = t.Trim().Trim('"');
            if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) t = t[7..].Trim();
            return t;
        }

        private static JsonSerializerOptions JsonOpts => new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private HttpClient CreateClient(string? token)
        {
            var http = _factory.CreateClient("MdwApiBaseUrl");
            if (!string.IsNullOrWhiteSpace(token))
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", NormalizeToken(token));
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return http;
        }

        // ===== Auth (MDW) =====
        public async Task<LoginResponseDtos?> LoginAsync(string username, string password)
        {
            var payload = new { username, password };
            var res = await _http.PostAsJsonAsync("api/Auth/login", payload);
            if (!res.IsSuccessStatusCode) return null;

            var loginResponse = await res.Content.ReadFromJsonAsync<LoginResponseDtos>(JsonOpts);
            if (loginResponse != null)
            {
                await SetSessionAsync(loginResponse);
            }
            return loginResponse;
        }

        public async Task<bool> SetSessionAsync(LoginResponseDtos dto)
        {
            var res = await _http.PostAsJsonAsync("MdwToken/set-session", dto);
            return res.IsSuccessStatusCode;
        }

        // ===== Marketplace Auth Flow =====
        public async Task<(bool ok, string? authUrl, string? error)> GetShopeeAuthLinkAsync(
            string shopId,
            string callbackUrl,
            string? token = null,
            CancellationToken ct = default)
        {
            using var http = CreateClient(token);

            var qb = new QueryBuilder
            {
                { "shopId",      shopId },
                { "callbackUrl", callbackUrl }
            };
            var url = "/api/market/auth/shopee/link" + qb.ToQueryString();

            using var res = await http.GetAsync(url, ct);
            var raw = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                var msg = string.IsNullOrWhiteSpace(raw)
                    ? $"{(int)res.StatusCode} {res.ReasonPhrase}"
                    : raw;
                return (false, null, msg);
            }

            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("authUrl", out var au))
                    return (true, au.GetString(), null);
                return (false, null, $"Invalid response JSON: {raw}");
            }
            catch
            {
                return (false, null, $"Non-JSON response: {raw}");
            }
        }

        public async Task<(bool ok, string body)> ExchangeCodeAsync(
            string platform,
            string shopId,
            string code,
            string? token = null,
            CancellationToken ct = default)
        {
            using var http = CreateClient(token);

            var qb = new QueryBuilder
            {
                { "platform", platform },
                { "shopId",   shopId   },
                { "code",     code     }
            };
            var url = "/api/market/auth/exchange" + qb.ToQueryString();

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            using var res = await http.SendAsync(req, ct);
            var raw = await res.Content.ReadAsStringAsync(ct);

            return (res.IsSuccessStatusCode,
                    string.IsNullOrWhiteSpace(raw) ? $"{(int)res.StatusCode} {res.ReasonPhrase}" : raw);
        }

        // ===== Normalize wrappers =====
        public async Task<(bool ok, string body)> NormalizeByRefAsync(
            string platform,
            string shopId,
            string orderRef,
            string? token = null,
            CancellationToken ct = default)
        {
            using var http = CreateClient(token);

            var qb = new QueryBuilder
            {
                { "platform", platform },
                { "shopId",   shopId   },
                { "orderRef", orderRef }
            };
            var url = "/api/market/normalize/by-ref" + qb.ToQueryString();

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };
            using var res = await http.SendAsync(req, ct);
            var raw = await res.Content.ReadAsStringAsync(ct);

            return (res.IsSuccessStatusCode,
                    string.IsNullOrWhiteSpace(raw) ? $"{(int)res.StatusCode} {res.ReasonPhrase}" : raw);
        }

        public async Task<(bool ok, string body)> NormalizeByListAsync(
            string platform,
            string shopId,
            string timeRangeField,
            long timeFrom,
            long timeTo,
            int pageSize = 50,
            string? sellerId = null,
            string? env = null,
            string? token = null,
            CancellationToken ct = default)
        {
            using var http = CreateClient(token);

            var qb = new QueryBuilder
            {
                { "platform",       platform        },
                { "shopId",         shopId          },
                { "timeRangeField", timeRangeField  },
                { "timeFrom",       timeFrom.ToString() },
                { "timeTo",         timeTo.ToString()   },
                { "pageSize",       pageSize.ToString() }
            };
            if (!string.IsNullOrWhiteSpace(sellerId)) qb.Add("sellerId", sellerId);
            if (!string.IsNullOrWhiteSpace(env)) qb.Add("env", env);

            var url = "/api/market/normalize/by-list" + qb.ToQueryString();

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };
            using var res = await http.SendAsync(req, ct);
            var raw = await res.Content.ReadAsStringAsync(ct);

            return (res.IsSuccessStatusCode,
                    string.IsNullOrWhiteSpace(raw) ? $"{(int)res.StatusCode} {res.ReasonPhrase}" : raw);
        }

        // ===== Legacy order APIs (ถ้ายังใช้อยู่ที่อื่น) =====
        public async Task<(bool ok, string body)> GetOrderDetailAsync(
            string platform,
            string shopId,
            string orderRef,
            string? token = null,
            CancellationToken ct = default)
        {
            using var http = CreateClient(token);

            var qb = new QueryBuilder
            {
                { "platform", platform },
                { "shopId",   shopId   },
                { "orderRef", orderRef }
            };
            var url = "/api/market/orders/detail" + qb.ToQueryString();

            using var res = await http.GetAsync(url, ct);
            var raw = await res.Content.ReadAsStringAsync(ct);

            return (res.IsSuccessStatusCode,
                    string.IsNullOrWhiteSpace(raw) ? $"{(int)res.StatusCode} {res.ReasonPhrase}" : raw);
        }

        public async Task<(bool ok, string body)> GetOrderListAsync(
            string platform,
            string shopId,
            long fromUnix,
            long toUnix,
            string? orderStatus = null,
            int pageSize = 50,
            string? cursor = null,
            string? token = null,
            CancellationToken ct = default)
        {
            using var http = CreateClient(token);

            var qb = new QueryBuilder
            {
                { "platform",   platform        },
                { "shopId",     shopId          },
                { "from",       fromUnix.ToString() },
                { "to",         toUnix.ToString()   },
                { "pageSize",   pageSize.ToString() }
            };
            if (!string.IsNullOrWhiteSpace(orderStatus)) qb.Add("orderStatus", orderStatus);
            if (!string.IsNullOrWhiteSpace(cursor)) qb.Add("cursor", cursor);

            var url = "/api/market/orders/list" + qb.ToQueryString();

            using var res = await http.GetAsync(url, ct);
            var raw = await res.Content.ReadAsStringAsync(ct);

            return (res.IsSuccessStatusCode,
                    string.IsNullOrWhiteSpace(raw) ? $"{(int)res.StatusCode} {res.ReasonPhrase}" : raw);
        }

        // ===== FE Orders (MarketplaceFeOrdersController) =====
        public async Task<PagedResult<FeUnifiedOrderDtos>> FeOrdersListAsync(
            string? token,
            string? channel = null,
            long? shopId = null,
            string? buyerUserId = null,
            string? q = null,
            string? status = null,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            string? dateField = null,
            int page = 1,
            int pageSize = 50,
            string? sort = "createdDesc",
            CancellationToken ct = default)
        {
            using var http = CreateClient(token);

            var qb = new QueryBuilder();
            if (!string.IsNullOrWhiteSpace(channel)) qb.Add("channel", channel);
            if (shopId is > 0) qb.Add("shopId", shopId.Value.ToString());
            if (!string.IsNullOrWhiteSpace(buyerUserId)) qb.Add("buyerUserId", buyerUserId);
            if (!string.IsNullOrWhiteSpace(q)) qb.Add("q", q);
            if (!string.IsNullOrWhiteSpace(status)) qb.Add("status", status);
            if (fromUtc.HasValue) qb.Add("fromUtc", fromUtc.Value.ToUniversalTime().ToString("o"));
            if (toUtc.HasValue) qb.Add("toUtc", toUtc.Value.ToUniversalTime().ToString("o"));
            if (!string.IsNullOrWhiteSpace(dateField)) qb.Add("dateField", dateField);
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 50;
            qb.Add("page", page.ToString());
            qb.Add("pageSize", pageSize.ToString());
            if (!string.IsNullOrWhiteSpace(sort)) qb.Add("sort", sort);

            var url = "api/fe/orders" + qb.ToQueryString();

            using var res = await http.GetAsync(url, ct);
            res.EnsureSuccessStatusCode();

            var obj = await res.Content.ReadFromJsonAsync<PagedResult<FeUnifiedOrderDtos>>(JsonOpts, ct);
            return obj ?? new PagedResult<FeUnifiedOrderDtos>
            {
                Page = page,
                Size = pageSize,
                TotalItems = 0,
                Items = new List<FeUnifiedOrderDtos>()
            };
        }

        public async Task<FeUnifiedOrderDtos?> FeOrderGetByIdAsync(
            string? token,
            long id,
            CancellationToken ct = default)
        {
            using var http = CreateClient(token);
            using var res = await http.GetAsync($"api/fe/orders/{id}", ct);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<FeUnifiedOrderDtos>(JsonOpts, ct);
        }

        public async Task<FeUnifiedOrderDtos?> FeOrderGetByExternalAsync(
            string? token,
            string channel,
            string externalOrderNo,
            CancellationToken ct = default)
        {
            using var http = CreateClient(token);
            var url = $"api/fe/orders/by-external/{Uri.EscapeDataString(channel)}/{Uri.EscapeDataString(externalOrderNo)}";
            using var res = await http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<FeUnifiedOrderDtos>(JsonOpts, ct);
        }
        // ===== Shipping Labels =====
        public async Task<LabelListResponse> ListLabelsAsync(
            string? token,
            long? shopId = null,
            string? channel = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default)
        {
            using var http = CreateClient(token);

            var qb = new QueryBuilder();
            if (shopId is > 0) qb.Add("shopId", shopId.Value.ToString());
            if (!string.IsNullOrWhiteSpace(channel)) qb.Add("channel", channel);
            if (fromDate.HasValue) qb.Add("fromDate", fromDate.Value.ToString("yyyy-MM-dd"));
            if (toDate.HasValue) qb.Add("toDate", toDate.Value.AddDays(1).ToString("yyyy-MM-dd"));
            qb.Add("page", page.ToString());
            qb.Add("pageSize", pageSize.ToString());

            var url = "/api/market/orders/actions/list-labels" + qb.ToQueryString();

            using var res = await http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync(ct);
                throw new Exception($"ListLabels failed {(int)res.StatusCode}: {err}");
            }

            return await res.Content.ReadFromJsonAsync<LabelListResponse>(JsonOpts, ct)
                   ?? new LabelListResponse();
        }

        public async Task<(bool ok, string body)> ReloadLabelAsync(
            string? token,
            string platform,
            long shopId,
            string orderRef,
            CancellationToken ct = default)
        {
            using var http = CreateClient(token);

            var url = $"/api/market/orders/actions/reload-label?platform={Uri.EscapeDataString(platform)}&shopId={shopId}&orderRef={Uri.EscapeDataString(orderRef)}";

            using var res = await http.PostAsync(url, null, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            return (res.IsSuccessStatusCode, body);
        }

        public async Task<(bool ok, byte[]? data, string? contentType, string? fileName, string? error)> DownloadLabelAsync(
            string? token,
            string orderRef,
            CancellationToken ct = default)
        {
            using var http = CreateClient(token);

            var url = $"/api/market/orders/actions/download-label?orderRef={Uri.EscapeDataString(orderRef)}";

            using var res = await http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync(ct);
                return (false, null, null, null, $"{(int)res.StatusCode}: {err}");
            }

            var data = await res.Content.ReadAsByteArrayAsync(ct);
            var ct2 = res.Content.Headers.ContentType?.MediaType ?? "application/pdf";
            var fn = res.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                     ?? $"label_{orderRef}.pdf";

            return (true, data, ct2, fn, null);
        }
    }
}
