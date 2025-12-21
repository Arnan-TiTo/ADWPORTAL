using Microsoft.Extensions.Options;
using adwportal.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace adwportal.Services
{
    public class UserLocationApi
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IOptions<ApiSettings> _options;

        public UserLocationApi(IHttpClientFactory clientFactory, IOptions<ApiSettings> options)
        {
            _clientFactory = clientFactory;
            _options = options;
        }

        private Task<HttpClient> CreateClientAsync()
        {
            var apiBase = _options.Value.ApiBaseUrl?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(apiBase))
                throw new InvalidOperationException("ApiSettings.ApiBaseUrl is not configured.");

            var http = _clientFactory.CreateClient();
            http.BaseAddress = new Uri(apiBase + "/");

            var fixeDtosken = Environment.GetEnvironmentVariable("AuthToken", EnvironmentVariableTarget.Machine);
            if (!string.IsNullOrWhiteSpace(fixeDtosken))
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixeDtosken);

            return Task.FromResult(http);
        }

        public async Task<List<LocationOption>> GetLocationOptionsAsync()
        {
            var http = await CreateClientAsync();
            var data = await http.GetFromJsonAsync<List<LocationOption>>("api/locations/dropdown");
            return data ?? new List<LocationOption>();
        }

        public async Task<List<UserLocationDtos>> GetUserLocationsAsync(int userId)
        {
            var http = await CreateClientAsync();
            var data = await http.GetFromJsonAsync<List<UserLocationDtos>>($"api/userlocations/user/{userId}");
            return data ?? new List<UserLocationDtos>();
        }

        public async Task<bool> AddAsync(int userId, int locationId)
        {
            var http = await CreateClientAsync();
            var res = await http.PostAsJsonAsync("api/userlocations", new UserLocationDtos { UserId = userId, LocationId = locationId });
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(int userId, int locationId)
        {
            var http = await CreateClientAsync();
            var res = await http.DeleteAsync($"api/userlocations/user/{userId}/location/{locationId}");
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> ReplaceAllAsync(int userId, IEnumerable<int> locationIds)
        {
            var http = await CreateClientAsync();
            var body = new UpdateUserLocationsRequest { LocationIds = locationIds?.Distinct().ToArray() ?? Array.Empty<int>() };
            var res = await http.PutAsJsonAsync($"api/userlocations/user/{userId}", body);
            return res.IsSuccessStatusCode;
        }
    }
}
