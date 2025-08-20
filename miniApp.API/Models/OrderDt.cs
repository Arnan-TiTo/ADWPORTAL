using System.ComponentModel.DataAnnotations.Schema;

namespace miniApp.API.Models
{
    public class OrderDt
    {
        public int Id { get; set; }

        public int OrderHdId { get; set; }
        public OrderHd? OrderHd { get; set; }

        public int LocationId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total => (UnitPrice * Quantity) - Discount;
    }
}
