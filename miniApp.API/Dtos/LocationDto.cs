using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace miniApp.API.Dtos
{
    public class LocationDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Note { get; set; } = string.Empty;
        public float Latitude { get; set; } = 0;
        public float Longitude { get; set; } = 0;
        public List<IFormFile> Image { get; set; } = new();
    }
}
