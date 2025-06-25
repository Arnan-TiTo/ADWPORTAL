using Mono.TextTemplating;
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
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = "staff"; // 'admin', 'staff', etc.

        public ICollection<Location>? Locations { get; set; }
    }
}
