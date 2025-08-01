using Microsoft.AspNetCore.Http;  
using Microsoft.Extensions.Configuration;
using miniApp.Web.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace miniApp.Web.Services
{
    public class LocationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Constructor
        public LocationService(HttpClient httpClient, IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _config = config;
            _httpContextAccessor = httpContextAccessor; 
        }

        public async Task<bool> CreateLocationAsync(LocationDto location)
        {
            var apiBase = _config["ApiBaseUrl"] ?? "";
            _httpClient.BaseAddress = new System.Uri(apiBase);

            var form = new MultipartFormDataContent
            {
                { new StringContent(location.Name), "Name" },
                { new StringContent(location.Note ?? string.Empty), "Note" },
                { new StringContent(location.Latitude.ToString()), "Latitude" },
                { new StringContent(location.Longitude.ToString()), "Longitude" }
            };
    
            if (location.Image != null && location.Image.Length > 0)
            {
                try
                {
                    var streamContent = new StreamContent(location.Image.OpenReadStream());
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(location.Image.ContentType);
                    form.Add(streamContent, "ImageFiles", location.Image.FileName);
                }
                catch (Exception ex)
                {
                    throw new Exception("Image upload failed: " + ex.Message);
                }
            }

            try
            {
                var response = await _httpClient.PostAsync("api/locations", form);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to submit location: " + ex.Message);
            }
        }
    }
}
