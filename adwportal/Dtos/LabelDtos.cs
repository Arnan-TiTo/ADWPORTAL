namespace adwportal.Dtos;

/// <summary>
/// Matches the anonymous object returned by list-labels API:
/// { total, page, pageSize, items: [ { Id, Channel, ShopId, OrderExternalNo, Location, FileName, DocumentType, FileSizeBytes, CreatedDate } ] }
/// </summary>
public record LabelListResponse
{
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public List<LabelItemDtos> Items { get; init; } = new();
}

public record LabelItemDtos
{
    public long Id { get; init; }
    public string? Channel { get; init; }
    public long ShopId { get; init; }
    public string? OrderExternalNo { get; init; }
    public string? Location { get; init; }
    public string? FileName { get; init; }
    public string? DocumentType { get; init; }
    public long FileSizeBytes { get; init; }
    public DateTime? CreatedDate { get; init; }
}
