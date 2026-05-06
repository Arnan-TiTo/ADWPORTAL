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

public record ShippingDocDataInfoDtos
{
    public long Id { get; init; }
    public string? Platform { get; init; }
    public long ShopId { get; init; }
    public string? OrderSn { get; init; }
    public string? PackageNumber { get; init; }
    public string? Status { get; init; }
    public string? FailError { get; init; }
    public string? FailMessage { get; init; }
    public string? DocumentType { get; init; }
    public string? TrackingNumber { get; init; }
    public string? LogisticsChannelName { get; init; }
    public decimal? CodAmount { get; init; }
    public int? ProductCount { get; init; }
    public string? PurchaseOrderNumber { get; init; }
    public string? PurchaseOrderPrefix { get; init; }
    public string? BuyerRemark { get; init; }
    public long? ParcelChargeableWeightGram { get; init; }
    public decimal? BillingWeight { get; init; }
    public string? SenderName { get; init; }
    public string? SenderPhone { get; init; }
    public string? SenderFullAddress { get; init; }
    public string? SenderZipcode { get; init; }
    public string? RecipientName { get; init; }
    public string? RecipientPhone { get; init; }
    public string? RecipientTown { get; init; }
    public string? RecipientDistrict { get; init; }
    public string? RecipientCity { get; init; }
    public string? RecipientState { get; init; }
    public string? RecipientRegion { get; init; }
    public string? RecipientZipcode { get; init; }
    public string? RecipientFullAddress { get; init; }
    public string? AsSortCode { get; init; }
    public string? CpSortCode { get; init; }
    public string? FmSortCode { get; init; }
    public string? QrCodeData { get; init; }
    public string? ItemListJson { get; init; }
    public string? RawJson { get; init; }
    public string? RecipientAddrRawJson { get; init; }
    public string? RecipientAddrDecodedJson { get; init; }
    public string? SenderAddrRawJson { get; init; }
    public string? SenderAddrDecodedJson { get; init; }
    public DateTime? FetchedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
