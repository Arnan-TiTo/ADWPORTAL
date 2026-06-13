using System.Text.Json.Serialization;

namespace adwportal.Dtos;

public sealed class NormalizeReloadStatusLookupRequestDtos
{
    [JsonPropertyName("shopId")]
    public long ShopId { get; set; }

    [JsonPropertyName("externalOrder")]
    public string ExternalOrder { get; set; } = "";
}

public sealed class NormalizeReloadStatusDtos
{
    [JsonPropertyName("shopId")]
    public long ShopId { get; set; }

    [JsonPropertyName("externalOrder")]
    public string ExternalOrder { get; set; } = "";

    [JsonPropertyName("normalizeReloadStatus")]
    public string? NormalizeReloadStatus { get; set; }

    [JsonPropertyName("attemptCount")]
    public int? AttemptCount { get; set; }

    [JsonPropertyName("lastErrorMessage")]
    public string? LastErrorMessage { get; set; }
}
