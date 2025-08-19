
using miniApp.API.Models;
using System;

namespace miniApp.API.Dtos
{
    public class UserLocationDto
    {
        public int UserId { get; set; }
        public int LocationId { get; set; }
        public User? User { get; set; }
        public Location? Location { get; set; }
    }
    public class UpdateUserLocationsDto
    {
        public int[] LocationIds { get; set; } = Array.Empty<int>();
    }

}
