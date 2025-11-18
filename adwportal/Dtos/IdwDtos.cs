namespace adwportal.Dtos;


public class PagedResult<T>
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => Size <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)Size);
    public List<T> Items { get; set; } = new();
}

public class IdwOrderDtos
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

public class LoginResponseDtos
{
    public string Token { get; set; } = "";
    public int expires_in_hours { get; set; }
}

/* ====== UPDATED: รองรับผลสรุปการ import ใหม่จาก API ====== */
public class UploadResponseDtos
{
    public long importId { get; set; }
    public int rows { get; set; }
    public string? batchNo { get; set; }
    public int skippedDb { get; set; }
    public int skippedFile { get; set; }
    public List<SkipItem> skippedOrders { get; set; } = new();
}

public class SkipItem
{
    public string? OrderNo { get; set; }
    public string Reason { get; set; } = "";
    public string? Detail { get; set; }
}

/* ====== ข้อมูล import และแถว ====== */
public class IdwImportDtos
{
    public long Id { get; set; }
    public string SourceType { get; set; } = "Text";
    public string? FileName { get; set; } = string.Empty;
    public string? BatchNo { get; set; }
    public string ImportedBy { get; set; } = "system";
    public DateTime ImportedAt { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Pending"; // Pending|Parsed|Error|IMPORTED
    public string? ErrorMessage { get; set; }
    public string? RawMetaJson { get; set; }
    public int RowCount { get; set; }
    public int? CompanyId { get; set; }
    public int? MiscIdPlatform { get; set; }
    public int? MiscIdLogistic { get; set; }
    public string? CompanyName { get; set; }
    public string? PlatformName { get; set; }
    public string? LogisticName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ICollection<IdwImportRowDtos> Rows { get; set; } = new List<IdwImportRowDtos>();
}

// === แถวในไฟล์ (ตัวจริงที่ใช้ทั้งโหลด/แก้ไข) ===
public class IdwImportRowDtos
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
