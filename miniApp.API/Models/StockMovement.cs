using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace miniApp.API.Models
{
    public class StockMovement
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public string Type { get; set; } = "in"; // 'in', 'adjust', 'count'

        public int Quantity { get; set; }
        public string? Note { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
