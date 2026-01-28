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

        private async Task<string?> GetMdwApiTokenAsync(CancellationToken ct = default)
        {
            try
            {
                var client = _factory.CreateClient("MdwApiBaseUrl");
                var response = await client.PostAsJsonAsync("/api/Auth/login",
                    new { username = "admin", password = "123456yjm" }, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("MDWAPI login failed: {Status}", response.StatusCode);
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
                _logger.LogError(ex, "Failed to get MDWAPI token");
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
            var idwOk = await SyncToTargetAsync("IdwApiBaseUrl", "POST", username, password, role, isActive);
            var mdwOk = await SyncToTargetAsync("MdwApiBaseUrl", "POST", username, password, role, isActive);
            return idwOk || mdwOk;
        }

        public async Task<bool> SyncUpdateUserAsync(
            int userId,
            string username,
            string? password,
            string role,
            bool isActive)
        {
            var idwOk = await SyncToTargetAsync("IdwApiBaseUrl", "PUT", username, password, role, isActive);
            var mdwOk = await SyncToTargetAsync("MdwApiBaseUrl", "PUT", username, password, role, isActive);
            return idwOk || mdwOk;
        }

        private async Task<bool> SyncToTargetAsync(
            string clientName,
            string method,
            string username,
            string? password,
            string role,
            bool isActive)
        {
            try
            {
                string? token = clientName == "IdwApiBaseUrl" 
                    ? await GetIdwApiTokenAsync() 
                    : await GetMdwApiTokenAsync();

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Cannot sync user {Method} to {Target} - no token", method, clientName);
                    return false;
                }

                var client = _factory.CreateClient(clientName);
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // Dictionary keys in lowerCamelCase to match standard JSON policies used in target APIs
                var payload = new System.Collections.Generic.Dictionary<string, object?>
                {
                    { "username", username },
                    { "role", role },
                    { "isActive", isActive } // bool
                };

                if (!string.IsNullOrEmpty(password))
                {
                    payload.Add("password", password);
                }

                HttpResponseMessage response;
                if (method == "POST")
                {
                    response = await client.PostAsJsonAsync("/api/users", payload);
                }
                else
                {
                    response = await client.PutAsJsonAsync($"/api/users/by-username/{Uri.EscapeDataString(username)}", payload);
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        if (!string.IsNullOrEmpty(password))
                             response = await client.PostAsJsonAsync("/api/users", payload);
                        else
                        {
                            _logger.LogWarning("Fallback POST failed for {Username} on {Target} - no password provided", username, clientName);
                            return false;
                        }
                    }
                }

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("User {Method} synced to {Target}: {Username}", method, clientName, username);
                    return true;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("{Target} user {Method} failed: {Status} - {Error}",
                    clientName, method, response.StatusCode, error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user {Method} to {Target}: {Username}", method, clientName, username);
                return false;
            }
        }

        public async Task<bool> SyncDeleteUserAsync(int userId)
        {
            var idwOk = await SyncDeleteFromTargetAsync("IdwApiBaseUrl", userId);
            var mdwOk = await SyncDeleteFromTargetAsync("MdwApiBaseUrl", userId);
            return idwOk || mdwOk;
        }

        private async Task<bool> SyncDeleteFromTargetAsync(string clientName, int userId)
        {
            try
            {
                string? token = clientName == "IdwApiBaseUrl" 
                    ? await GetIdwApiTokenAsync() 
                    : await GetMdwApiTokenAsync();

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Cannot sync user delete to {Target} - no token", clientName);
                    return false;
                }

                var client = _factory.CreateClient(clientName);
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var response = await client.DeleteAsync($"/api/users/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("User delete synced to {Target} (ID: {Id})", clientName, userId);
                    return true;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("{Target} user delete failed: {Status} - {Error}",
                    clientName, response.StatusCode, error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user delete to {Target} (ID: {Id})", clientName, userId);
                return false;
            }
        }
    }
}
