namespace miniApp.API.Dtos
{
    public class ProductStockDto
    {
        public int UserId { get; set; }
        public int LocationId { get; set; }
    }

    public class AdjustStockDto
    {
        public int ProductId { get; set; }
        public int LocationId { get; set; }
        /// <summary>
        /// >0 รับเข้า, <0 ตัดออก (single-leg)
        /// </summary>
        public int QtyOnHand { get; set; }
        public string? ReasonCode { get; set; }
        public string? ReferenceType { get; set; }
        public string? ReferenceId { get; set; }
        public int? PerformedByUserId { get; set; }
        public string? Note { get; set; }
    }

    public class TransferStockDto
    {
        public int ProductId { get; set; }
        public int FromLocationId { get; set; }
        public int ToLocationId { get; set; }
        public int QtyOnHand { get; set; } 
        public string? ReasonCode { get; set; }
        public string? ReferenceType { get; set; }
        public string? ReferenceId { get; set; }
        public int? PerformedByUserId { get; set; }
        public string? Note { get; set; }
    }

    public class ReserveDto
    {
        public int ProductId { get; set; }
        public int LocationId { get; set; }
        public int QtyOnHand { get; set; }
        public string? ReasonCode { get; set; }
        public string? ReferenceType { get; set; }
        public string? ReferenceId { get; set; }
        public int? PerformedByUserId { get; set; }
        public string? Note { get; set; }
    }

    public class DamageDto
    {
        public int ProductId { get; set; }
        public int LocationId { get; set; }
        public int QtyOnHand { get; set; }
        public string? ReasonCode { get; set; }
        public string? ReferenceType { get; set; }
        public string? ReferenceId { get; set; }
        public int? PerformedByUserId { get; set; }
        public string? Note { get; set; }
    }
    public class AddStockRowDto
    {
        public int ProductId { get; set; }
        public int LocationId { get; set; }
        public int InitialQty { get; set; } = 0;
        public string? ReasonCode { get; set; }
        public string? ReferenceType { get; set; }
        public string? ReferenceId { get; set; }
        public int? PerformedByUserId { get; set; }
        public string? Note { get; set; }
        public decimal? Cost { get; set; }
    }
}
