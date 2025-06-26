using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace miniApp.API.Models
{
    public enum RoleType
    {
        Admin,
        Staff
    }

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

        public RoleType Role { get; set; } = RoleType.Staff; // ใช้ enum สำหรับ Role

        public ICollection<Location>? Locations { get; set; }
    }
}
