using miniApp.API.Models;
using System;

namespace miniApp.API.Dtos
{
    public class StockTransactionsDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int FromLocationId { get; set; }
        public int ToLocationId { get; set; }
        public Location? Location { get; set; }
        public int QtyChange { get; set; }
        public string ReasonCode { get; set; } = string.Empty;
        public string ReferenceType { get; set; } = string.Empty;
        public string ReferenceId { get; set; } = string.Empty;
        public decimal UnitCost { get; set; }
        public int PerformedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Note { get; set; } = string.Empty;
    }
}
