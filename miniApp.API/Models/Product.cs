using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace miniApp.API.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Sku { get; set; }

        public string? Description { get; set; }

        public int Quantity { get; set; }

        public string? Note { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key to Location
        [ForeignKey("Location")]
        public int LocationId { get; set; }
        public Location? Location { get; set; }

        // Foreign key to User
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User? User { get; set; }

        // Navigation properties
        public ICollection<Inventory>? Inventories { get; set; }
        public ICollection<StockMovement>? StockMovements { get; set; }
        public int? CategoryId { get; set; }
        public ProductCategory? Category { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        public int? BrandId { get; set; }
        public ProductBrand? Brand { get; set; }

    }
}
