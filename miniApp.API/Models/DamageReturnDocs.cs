using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace miniApp.API.Models
{
    [Table("DamageReturnDocs")]
    public class DamageReturnDocs
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string DocNo { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int FromLocationId { get; set; }

        [Required]
        public int ToLocationId { get; set; }

        [Required]
        public int Qty { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "PENDING"; // PENDING | CONFIRMED | CANCELLED

        [MaxLength(500)]
        public string? Note { get; set; }

        public int? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? ConfirmedByUserId { get; set; }
        public DateTime? ConfirmedAt { get; set; }

        public Product? Product { get; set; }
        public Location? FromLocation { get; set; }
        public Location? ToLocation { get; set; }
        public User? CreatedByUser { get; set; }
        public User? ConfirmedByUser { get; set; }
    }
}
