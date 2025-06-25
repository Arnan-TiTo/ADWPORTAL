using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace miniApp.API.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public ICollection<Inventory>? Inventories { get; set; }
        public ICollection<StockMovement>? StockMovements { get; set; }
    }
}
