using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace miniApp.API.Dtos
{
    // สำหรับรับค่า (สร้าง/แก้ไข)
    public class LocationDto
    {
        public string Name { get; set; } = "";
        public string? Note { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public List<IFormFile> Image { get; set; } = new();
        public string Usernames { get; set; } = "";
    }

    public class LocationUpdateDto : LocationDto { }

    public class LocationImageDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = "";
    }

    public class LocationResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Note { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public DateTime CreatedAt { get; set; }

        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string Fullname { get; set; } = "";

        public List<LocationImageDto> Images { get; set; } = new();
    }
}
