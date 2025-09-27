namespace adwportal.Dtos
{
    public record MiscItemDtos(int Id, string Value1);

    public class MiscDtos
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string? Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Value1 { get; set; } = string.Empty;
        public string? Value2 { get; set; } = string.Empty;
        public string? Value3 { get; set; } = string.Empty;
        public string? Value4 { get; set; } = string.Empty;
        public string? Value5 { get; set; } = string.Empty;
        public string? Note { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class MiscUpsertDtos
    {
        public int? ParentId { get; set; }
        public string? Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Value1 { get; set; } = string.Empty;
        public string? Value2 { get; set; } = string.Empty;
        public string? Value3 { get; set; } = string.Empty;
        public string? Value4 { get; set; } = string.Empty;
        public string? Value5 { get; set; } = string.Empty;
        public string? Note { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

}
