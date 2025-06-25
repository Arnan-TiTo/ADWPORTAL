using System.ComponentModel.DataAnnotations.Schema;

namespace miniApp.API.Models
{
    public class LocationImage
    {
        public int Id { get; set; }

        public int LocationId { get; set; }

        [ForeignKey("LocationId")]
        public Location? Location { get; set; }

        public string ImageUrl { get; set; } = string.Empty;
    }
}
