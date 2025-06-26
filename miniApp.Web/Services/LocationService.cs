using Microsoft.AspNetCore.Http;  // สำหรับ IHttpContextAccessor
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
        private readonly IHttpContextAccessor _httpContextAccessor;  // Add IHttpContextAccessor

        // Constructor
        public LocationService(HttpClient httpClient, IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _config = config;
            _httpContextAccessor = httpContextAccessor;  // Inject IHttpContextAccessor
        }

        // ฟังก์ชันเพื่อเพิ่มข้อมูลตำแหน่งใหม่
        public async Task<bool> CreateLocationAsync(LocationDto location)
        {
            // ดึง JWT token จาก cookies
            //var httpContext = _httpContextAccessor.HttpContext;
            //if (httpContext == null)
            //{
            //    throw new InvalidOperationException("HttpContext is not available.");
            //}

            //var token = httpContext.Request.Cookies["MiniApp.Auth"]; // Access the cookie
            //if (string.IsNullOrEmpty(token))
            //{
            //    throw new UnauthorizedAccessException("Authorization required. Please log in.");
            //}

            //// เพิ่ม Authorization header
            //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiBase = _config["ApiBaseUrl"] ?? "http://localhost:5252";
            _httpClient.BaseAddress = new System.Uri(apiBase);

            var form = new MultipartFormDataContent
            {
                { new StringContent(location.Name), "Name" },
                { new StringContent(location.Note ?? string.Empty), "Note" },
                { new StringContent(location.Latitude.ToString()), "Latitude" },
                { new StringContent(location.Longitude.ToString()), "Longitude" }
            };

            // ถ้ามีไฟล์ที่ต้องการอัพโหลด
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
                    Console.WriteLine($"Image upload failed: {ex.Message}");  // เพิ่มข้อความแสดงข้อผิดพลาด
                    throw new Exception("Image upload failed: " + ex.Message);
                }
            }

            try
            {
                // ใช้ _httpClient ส่งคำขอ POST ไปยัง API
                var response = await _httpClient.PostAsync("/api/locations", form); // ตรวจสอบ URL ใน API
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to submit location: {content}"); // เพิ่มการ log
                    return false;
                }

                Console.WriteLine($"Location saved: {content}"); // เพิ่มการ log
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to submit location: {ex.Message}");  // เพิ่ม log
                throw new Exception("Failed to submit location: " + ex.Message);
            }
        }
    }
}
