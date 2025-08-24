using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace miniApp.API.Models
{
    public class Location
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Note { get; set; } = string.Empty;
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<LocationImage> Images { get; set; } = new List<LocationImage>();
        public string? PlaceName { get; set; } = string.Empty;
        public string? Building { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;
        public string? District { get; set; } = string.Empty;
        public string? Province { get; set; } = string.Empty;
        public string? Postcode { get; set; } = string.Empty;
        public string? ContractPerson { get; set; } = string.Empty;
        public string? ContractPhone { get; set; } = string.Empty;
        public int? isWarehouse { get; set; } = 0;
        public int? isStorehouse { get; set; } = 0;
        public int? isDamagehouse { get; set; } = 0;

        public ICollection<UserLocation> UserLocations { get; set; } = new List<UserLocation>();
    }
}
