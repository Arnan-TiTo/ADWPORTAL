using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;

namespace adwportal.Services
{
    public class TokenCacheService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenCacheService> _logger;

        // In-memory cache
        private string? _mdwToken;
        private DateTime? _mdwTokenExpiry;
        private string? _idwToken;
        private DateTime? _idwTokenExpiry;

        private readonly SemaphoreSlim _mdwLock = new(1, 1);
        private readonly SemaphoreSlim _idwLock = new(1, 1);

        public System.Collections.Concurrent.ConcurrentBag<string> DebugLog { get; } = new();

        public TokenCacheService(
            IHttpClientFactory httpClientFactory,
            IServiceProvider serviceProvider,
            ILogger<TokenCacheService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<string?> GetMdwTokenAsync(string? username = null, string? password = null)
        {
            await _mdwLock.WaitAsync();
            try
            {
                // Check if cached token is still valid
                if (!string.IsNullOrEmpty(_mdwToken) && _mdwTokenExpiry.HasValue && DateTime.Now < _mdwTokenExpiry.Value)
                {
                    _logger.LogInformation("[TokenCache] Using cached MDW token (expires: {Expiry})", _mdwTokenExpiry);
                    return _mdwToken;
                }

                // Token expired or not cached, fetch new one
                _logger.LogInformation("[TokenCache] Fetching new MDW token...");
                var token = await FetchMdwTokenAsync(username, password);
                
                if (!string.IsNullOrEmpty(token))
                {
                    _mdwToken = token;
                    // Set expiry to 23 hours from now (tokens usually last 24h)
                    _mdwTokenExpiry = DateTime.Now.AddHours(23);
                    _logger.LogInformation("[TokenCache] MDW token cached (expires: {Expiry})", _mdwTokenExpiry);
                }

                return token;
            }
            finally
            {
                _mdwLock.Release();
            }
        }

        public async Task<string?> GetIdwTokenAsync(string? username = null, string? password = null)
        {
            await _idwLock.WaitAsync();
            try
            {
                // Check if cached token is still valid
                if (!string.IsNullOrEmpty(_idwToken) && _idwTokenExpiry.HasValue && DateTime.Now < _idwTokenExpiry.Value)
                {
                    _logger.LogInformation("[TokenCache] Using cached IDW token (expires: {Expiry})", _idwTokenExpiry);
                    return _idwToken;
                }

                // Token expired or not cached, fetch new one
                _logger.LogInformation("[TokenCache] Fetching new IDW token...");
                var token = await FetchIdwTokenAsync(username, password);
                
                if (!string.IsNullOrEmpty(token))
                {
                    _idwToken = token;
                    // Set expiry to 23 hours from now
                    _idwTokenExpiry = DateTime.Now.AddHours(23);
                    _logger.LogInformation("[TokenCache] IDW token cached (expires: {Expiry})", _idwTokenExpiry);
                }

                return token;
            }
            finally
            {
                _idwLock.Release();
            }
        }

        private async Task<string?> FetchMdwTokenAsync(string? argUsername = null, string? argPassword = null)
        {
            try
            {
                // Resolve scoped services from service provider
                using var scope = _serviceProvider.CreateScope();
                var tokenProvider = scope.ServiceProvider.GetRequiredService<AuthTokenProvider>();

                string? username = null;
                string? password = null;

                // Priority: Explicit Args > Context
                if (!string.IsNullOrEmpty(argUsername) && !string.IsNullOrEmpty(argPassword))
                {
                    username = argUsername;
                    password = argPassword;
                }
                else
                {
                    // Hybrid User Retrieval: Try AuthStateProvider (Blazor) first, then HttpContextAccessor (API)
                    ClaimsPrincipal? user = null;
                    try
                    {
                        var authStateProvider = scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();
                        var authState = await authStateProvider.GetAuthenticationStateAsync();
                        user = authState.User;
                    }
                    catch (InvalidOperationException)
                    {
                        // Fallback for non-Razor context (e.g. Program.cs)
                        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                        user = httpContextAccessor.HttpContext?.User;
                    }

                    if (user?.Identity?.IsAuthenticated != true)
                    {
                        _logger.LogWarning("[TokenCache] User not authenticated (Hybrid check failed)");
                        // DebugLog.Add("[Mdw] User not authenticated");
                        return null;
                    }

                    username = user?.FindFirst(ClaimTypes.Name)?.Value ?? user?.FindFirst("USERNAME")?.Value;
                    // Try getting password from CLaims first (works in Blazor), then fall back to TokenProvider (Session)
                    password = user?.FindFirst("PASSWORD")?.Value ?? tokenProvider.Password;
                }

                // DebugLog.Add($"[Mdw] User: {username}, HasPwd: {!string.IsNullOrEmpty(password)}");

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("[TokenCache] Missing username or password. Username: {Username}, HasPassword: {HasPassword}", username, !string.IsNullOrEmpty(password));
                    // DebugLog.Add("[Mdw] Missing username or password");
                    return null;
                }

                _logger.LogInformation("[TokenCache] Attempting MDW login for user: {Username}", username);
                var client = _httpClientFactory.CreateClient("MdwApiBaseUrl");
                var response = await client.PostAsJsonAsync("/api/Auth/login", new
                {
                    username,
                    password
                });

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("[TokenCache] MDW login failed: {Status}, Error: {Error}", response.StatusCode, errorContent);
                    // DebugLog.Add($"[Mdw] Failed {response.StatusCode}: {errorContent}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                // DebugLog.Add($"[Mdw] Success JSON: {json}");

                // var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                // Manual deserialize for debugging
                 var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                 var result = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(json, options);

                _logger.LogInformation("[TokenCache] MDW login successful");
                // DebugLog.Add($"[Mdw] Token parsed: {result?.Token}");
                return result?.Token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TokenCache] Error fetching MDW token");
                return null;
            }
        }

        private async Task<string?> FetchIdwTokenAsync(string? argUsername = null, string? argPassword = null)
        {
            try
            {
                // Resolve scoped services from service provider
                using var scope = _serviceProvider.CreateScope();
                var tokenProvider = scope.ServiceProvider.GetRequiredService<AuthTokenProvider>();

                string? username = null;
                string? password = null;

                // Priority: Explicit Args > Context
                if (!string.IsNullOrEmpty(argUsername) && !string.IsNullOrEmpty(argPassword))
                {
                    username = argUsername;
                    password = argPassword;
                }
                else
                {
                    // Hybrid User Retrieval: Try AuthStateProvider (Blazor) first, then HttpContextAccessor (API)
                    ClaimsPrincipal? user = null;
                    try
                    {
                        var authStateProvider = scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();
                        var authState = await authStateProvider.GetAuthenticationStateAsync();
                        user = authState.User;
                    }
                    catch (InvalidOperationException)
                    {
                        // Fallback for non-Razor context (e.g. Program.cs)
                        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                        user = httpContextAccessor.HttpContext?.User;
                    }

                    if (user?.Identity?.IsAuthenticated != true)
                    {
                        _logger.LogWarning("[TokenCache] User not authenticated (Hybrid check failed - IDW)");
                        return null;
                    }

                    username = user?.FindFirst(ClaimTypes.Name)?.Value ?? user?.FindFirst("USERNAME")?.Value;
                    // Try getting password from CLaims first (works in Blazor), then fall back to TokenProvider (Session)
                    password = user?.FindFirst("PASSWORD")?.Value ?? tokenProvider.Password;
                }

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("[TokenCache] Missing username or password. Username: {Username}, HasPassword: {HasPassword}", username, !string.IsNullOrEmpty(password));
                    return null;
                }

                _logger.LogInformation("[TokenCache] Attempting IDW login for user: {Username}", username);
                var client = _httpClientFactory.CreateClient("IdwApiBaseUrl");
                var response = await client.PostAsJsonAsync("/api/Auth/login", new
                {
                    username,
                    password
                });

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("[TokenCache] IDW login failed: {Status}, Error: {Error}", response.StatusCode, errorContent);
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                _logger.LogInformation("[TokenCache] IDW login successful");
                return result?.Token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TokenCache] Error fetching IDW token");
                return null;
            }
        }

        public void ClearCache()
        {
            _mdwToken = null;
            _mdwTokenExpiry = null;
            _idwToken = null;
            _idwTokenExpiry = null;
            _logger.LogInformation("[TokenCache] Cache cleared");
        }

        private class LoginResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("token")]
            public string? Token { get; set; }
        }
    }
}
