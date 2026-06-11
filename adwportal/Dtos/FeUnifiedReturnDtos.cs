using System.Text.Json;

namespace adwportal.Dtos;

public sealed class FeUnifiedReturnDtos
{
    public long Id { get; set; }
    public long UnifiedReturnId { get; set; }
    public string? Channel { get; set; }
    public long? ShopId { get; set; }

    public string? OrderRef { get; set; }
    public string? OrderSn { get; set; }
    public string? ExternalOrderNo { get; set; }

    public string? ReturnRef { get; set; }
    public string? ReturnSn { get; set; }
    public string? ExternalReturnId { get; set; }

    public string? Status { get; set; }
    public string? ReturnStatus { get; set; }
    public string? Reason { get; set; }
    public string? BuyerUsername { get; set; }

    public DateTime? CreatedTimeUtc { get; set; }
    public DateTime? UpdatedTimeUtc { get; set; }
    public DateTime? IngestedTimeUtc { get; set; }
    public DateTime? IngestedAtUtc { get; set; }

    public JsonElement Items { get; set; }
    public JsonElement ItemsJson { get; set; }
    public JsonElement RawJson { get; set; }
}
