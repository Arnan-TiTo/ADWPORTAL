using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace miniApp.API.Dtos
{
    public class LocationDto
    {
        public string Name { get; set; } = "";
        public string? Note { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public List<IFormFile> Image { get; set; } = new();
        public string Usernames { get; set; } = "";
        public string? PlaceName { get; set; } = string.Empty;
        public string? Building { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;
        public string? District { get; set; } = string.Empty;
        public string? Province { get; set; } = string.Empty;
        public string? Postcode { get; set; } = string.Empty;
        public string? ContractPerson { get; set; } = string.Empty;
        public string? ContractPhone { get; set; } = string.Empty;

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
        public string? PlaceName { get; set; }
        public string? Building { get; set; }
        public string? Address { get; set; }
        public string? District { get; set; }
        public string? Province { get; set; }
        public string? Postcode { get; set; }
        public string? ContractPerson { get; set; }
        public string? ContractPhone { get; set; }
        public List<LocationImageDto> Images { get; set; } = new();
    }

}
