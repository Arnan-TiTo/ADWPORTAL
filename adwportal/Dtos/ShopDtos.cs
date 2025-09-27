namespace adwportal.Dtos;

// Shops
public record ShopDtos(
    int Id,
    long ShopId,
    int PartnerId,
    string? Name,
    string? Country,
    DateTime CreatedAt
);

public record ShopUpsertDtos
{
    public long ShopId { get; set; }
    public int PartnerId { get; set; }
    public string? Name { get; set; }
    public string? Country { get; set; }
}
