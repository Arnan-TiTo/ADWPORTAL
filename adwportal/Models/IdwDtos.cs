namespace adwportal.Models;

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

/* ====== UPDATED: รองรับผลสรุปการ import ใหม่จาก API ====== */
public class UploadResponseDto
{
    public long importId { get; set; }              // เดิม int -> long ให้ตรง DB/API
    public int rows { get; set; }                  // จำนวนแถวที่ insert สำเร็จ
    public string? batchNo { get; set; }            // คงไว้เพื่อเข้ากันได้ (อาจว่างถ้า API ไม่ส่ง)
    public int skippedDb { get; set; }             // แถวที่ถูกข้ามเพราะมีอยู่แล้วใน DB
    public int skippedFile { get; set; }           // แถวที่ซ้ำในไฟล์เดียวกัน
    public List<SkipItem> skippedOrders { get; set; } = new(); // รายการ OrderNo ที่ข้าม
}

public class SkipItem
{
    public string? OrderNo { get; set; }
    public string Reason { get; set; } = "";      // 'exists_in_db' | 'duplicate_in_file' | อื่น ๆ
    public string? Detail { get; set; }            // เช่น page=3 หรือข้อความอธิบาย
}

/* ====== ข้อมูล import และแถว ====== */
public class IdwImportDto
{
    public long Id { get; set; }
    public string SourceType { get; set; } = "Text"; // Text|CSV|Excel|PDF
    public string? FileName { get; set; } = string.Empty;
    public string? BatchNo { get; set; }
    public string ImportedBy { get; set; } = "system";
    public DateTime ImportedAt { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Pending"; // Pending|Parsed|Error|IMPORTED
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
