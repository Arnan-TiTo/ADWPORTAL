using System.ComponentModel.DataAnnotations;

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

    public class IssueFromHeadDto
    {
        public int ProductId { get; set; }
        public int ToLocationId { get; set; }
        public int Qty { get; set; }
        public int? HeadLocationId { get; set; }
        public string? ReferenceType { get; set; }
        public string? ReferenceId { get; set; }
        public int? PerformedByUserId { get; set; }
        public string? Note { get; set; }
    }
    public class ReturnDamagedDto
    {
        public int ProductId { get; set; }
        public int FromLocationId { get; set; }
        public int Qty { get; set; }
        public int? HeadLocationId { get; set; }
        public string? ReferenceType { get; set; }
        public string? ReferenceId { get; set; }
        public int? PerformedByUserId { get; set; }
        public string? Note { get; set; }
    }
    public class CycleCountDto
    {
        public int LocationId { get; set; }
        public int ProductId { get; set; }
        public int CountedQty { get; set; }
        public string? ReasonCode { get; set; }
        public int? HeadLocationId { get; set; }
        public string? ReferenceType { get; set; }
        public string? ReferenceId { get; set; }
        public int? PerformedByUserId { get; set; }
        public string? Note { get; set; }
    }

    public class DamageReturnCreateDto
    {
        [Required] public int ProductId { get; set; }
        [Required] public int FromLocationId { get; set; }
        public int ToLocationId { get; set; } 
        [Required] public int Qty { get; set; }

        public string? Note { get; set; }
        public int? PerformedByUserId { get; set; }
        }


    public class DamageReturnConfirmDto
    {
        public int Id { get; set; }
        public int? PerformedByUserId { get; set; }
        public string? Note { get; set; }
    }

    public class MoveToDamagehouseDto
    {
        public int ProductId { get; set; }
        public int FromWarehouseId { get; set; }   // warehouse (isWarehouse=1)
        public int ToDamagehouseId { get; set; }   // damagehouse (isDamagehouse=1)
        public int Qty { get; set; }
        public int? PerformedByUserId { get; set; }
        public string? Note { get; set; }
    }
}
