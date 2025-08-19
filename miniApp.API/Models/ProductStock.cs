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
        public int MinLevel { get; set; } // Minimum stock level
        public int MaxLevel { get; set; } // Maximum stock level
        public int ReorderPoint { get; set; } // Point at which to reorder stock
        public decimal Cost { get; set; } // Cost of the product                                                
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Product? Product { get; set; }
        public Location? Location { get; set; }
    }
}

