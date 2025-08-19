using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace miniApp.API.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string Fullname { get; set; } = string.Empty;

        [Phone]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public ICollection<Location>? Locations { get; set; }
        
        [Required]
        public string QrLogin { get; set; } = string.Empty;
        public int isApproveQr { get; set; } = 0; // 0: not approved, 1: approved
        public int isActive { get; set; } = 0; // 0: not active, 1: active
        public int isDelete { get; set; } = 0; // 0: not active, 1: active

        public ICollection<UserLocation> UserLocations { get; set; } = new List<UserLocation>();

    }
}
