namespace adwportal.Dtos;

public sealed class NormalizeReloadStatusLookupRequestDtos
{
    public long ShopId { get; set; }
    public string ExternalOrder { get; set; } = "";
}

public sealed class NormalizeReloadStatusDtos
{
    public long ShopId { get; set; }
    public string ExternalOrder { get; set; } = "";
    public string? NormalizeReloadStatus { get; set; }
    public int? AttemptCount { get; set; }
    public string? LastErrorMessage { get; set; }
}
