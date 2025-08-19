using System;

namespace miniApp.API.Models
{
    public class ProductStock
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int LocationId { get; set; }
        public int QtyOnHand { get; set; } // >0
        public int QtyReserved { get; set; } // >0
        public int QtyDamaged { get; set; } // >0
        public int QtyAvailable { get; set; } // >0
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Product? Product { get; set; }
        public Location? Location { get; set; }
    }
}
