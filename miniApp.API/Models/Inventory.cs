using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace miniApp.API.Models
{
    public class Inventory
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public int Quantity { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
