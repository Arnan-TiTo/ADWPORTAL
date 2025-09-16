namespace miniApp.AdminPortal.Models;

public class PagedResult<T>
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => Size <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)Size);
    public List<T> Items { get; set; } = new();
}

public class IdwOrderDto
{
    public long Id { get; set; }
    public string? BatchNo { get; set; }
    public string? OrderNo { get; set; }
    public string? Sku { get; set; }
    public DateTime? PickupDate { get; set; }
    public DateTime? ShipByDate { get; set; }
    public string? SenderName { get; set; }
    public string? SenderAddress { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiveAddress { get; set; }
    public string? ProductName { get; set; }
    public string? Variant { get; set; }
    public string? Cod { get; set; }
    public int? Qty { get; set; }
}

public class LoginResponseDto
{
    public string Token { get; set; } = "";
    public int expires_in_hours { get; set; }
}

public class UploadResponseDto
{
    public int importId { get; set; }
    public int rows { get; set; }
    public string batchNo { get; set; } = string.Empty;
}

public class IdwImportDto
{
    public long Id { get; set; }
    public string SourceType { get; set; } = "Text"; // Text|CSV|Excel
    public string? FileName { get; set; } = string.Empty;
    public string? BatchNo { get; set; }
    public string ImportedBy { get; set; } = "system";
    public DateTime ImportedAt { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Pending"; // Pending|Parsed|Error
    public string? ErrorMessage { get; set; }
    public string? RawMetaJson { get; set; }
    public int RowCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<IdwImportRowDto> Rows { get; set; } = new List<IdwImportRowDto>();
}

// === แถวในไฟล์ (ตัวจริงที่ใช้ทั้งโหลด/แก้ไข) ===
public class IdwImportRowDto
{
    public long Id { get; set; }
    public long ImportId { get; set; }
    public int PageNo { get; set; }

    public string? OrderNo { get; set; }
    public DateTime? PickupDate { get; set; }
    public DateTime? ShipByDate { get; set; }
    public string? SenderName { get; set; }
    public string? SenderAddress { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverAddress { get; set; }
    public bool? IsCod { get; set; } 
    public string? ItemSdk { get; set; }
    public string? ItemName { get; set; }
    public string? ItemVariant { get; set; }
    public int? Qty { get; set; }

    public string RawText { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
