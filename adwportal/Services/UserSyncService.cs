using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;

namespace adwportal.Services
{
    public sealed class UserSyncService
    {
        private readonly IHttpClientFactory _factory;
        private readonly ILogger<UserSyncService> _logger;

        public UserSyncService(IHttpClientFactory factory, ILogger<UserSyncService> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        private async Task<string?> GetIdwApiTokenAsync(CancellationToken ct = default)
        {
            try
            {
                var client = _factory.CreateClient("IdwApiBaseUrl");
                var response = await client.PostAsJsonAsync("/api/Auth/login",
                    new { username = "admin", password = "123456yjm" }, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("IDWAPI login failed: {Status}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("token", out var tokenProp))
                    return tokenProp.GetString();

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get IDWAPI token");
                return null;
            }
        }

        public async Task<bool> SyncCreateUserAsync(
            int userId,
            string username,
            string password,
            string role,
            bool isActive)
        {
            try
            {
                var token = await GetIdwApiTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Cannot sync user create - no IDWAPI token");
                    return false;
                }

                var client = _factory.CreateClient("IdwApiBaseUrl");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var payload = new
                {
                    username,
                    password,
                    role,
                    isActive
                };

                var response = await client.PostAsJsonAsync("/api/users", payload);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("User synced to IDWAPI: {Username} (ID: {Id})", username, userId);
                    return true;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("IDWAPI user create failed: {Status} - {Error}",
                    response.StatusCode, error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user create to IDWAPI: {Username}", username);
                return false;
            }
        }

        public async Task<bool> SyncUpdateUserAsync(
            int userId,
            string username,
            string? password,
            string role,
            bool isActive)
        {
            try
            {
                _logger.LogInformation("[SYNC UPDATE] Starting sync for user {UserId}: username={Username}, hasPassword={HasPassword}, role={Role}, isActive={IsActive}", 
                    userId, username, !string.IsNullOrEmpty(password), role, isActive);

                var token = await GetIdwApiTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Cannot sync user update - no IDWAPI token");
                    return false;
                }

                var client = _factory.CreateClient("IdwApiBaseUrl");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // Build payload - only include password if it's provided
                var payload = string.IsNullOrEmpty(password)
                    ? new { username, role, isActive }
                    : (object)new { username, password, role, isActive };

                _logger.LogInformation("[SYNC UPDATE] Sending PUT to /api/users/by-username/{Username} with payload (hasPassword={HasPassword})", 
                    username, !string.IsNullOrEmpty(password));

                // Use username-based endpoint instead of ID
                var response = await client.PutAsJsonAsync($"/api/users/by-username/{Uri.EscapeDataString(username)}", payload);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("User update synced to IDWAPI: {Username}", username);
                    return true;
                }

                // If user not found (404), try to create it instead
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("User {Username} not found in IDWAPI, attempting to create instead", username);
                    
                    // Need password for create
                    if (string.IsNullOrEmpty(password))
                    {
                        _logger.LogWarning("Cannot create user in IDWAPI - password is required but not provided");
                        return false;
                    }

                    return await SyncCreateUserAsync(userId, username, password, role, isActive);
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("IDWAPI user update failed: {Status} - {Error}",
                    response.StatusCode, error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user update to IDWAPI: {Username}", username);
                return false;
            }
        }

        public async Task<bool> SyncDeleteUserAsync(int userId)
        {
            try
            {
                var token = await GetIdwApiTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Cannot sync user delete - no IDWAPI token");
                    return false;
                }

                var client = _factory.CreateClient("IdwApiBaseUrl");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var response = await client.DeleteAsync($"/api/users/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("User delete synced to IDWAPI (ID: {Id})", userId);
                    return true;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("IDWAPI user delete failed: {Status} - {Error}",
                    response.StatusCode, error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user delete to IDWAPI (ID: {Id})", userId);
                return false;
            }
        }
    }
}
