using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace miniApp.Web.Dtos
{
    public class LocationDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Note { get; set; } = string.Empty;
        public float Latitude { get; set; } = 0;
        public float Longitude { get; set; } = 0;
        public List<IFormFile> Image { get; set; } = new();
        public string Usernames { get; set; } = string.Empty;
    }

    public class LocationUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Note { get; set; } = string.Empty;
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public List<IFormFile> Image { get; set; } = new();
        public string Usernames { get; set; } = string.Empty;
    }

    public class LocationResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Note { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("s");
        public string Username { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new();
    }

}
