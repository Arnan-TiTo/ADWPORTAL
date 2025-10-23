namespace adwportal.Dtos;

public record ShopDtos(
    int Id,
    long ShopId,
    long PartnerId,
    string? Name,
    string? Country,
    DateTime CreatedAt,
    string? Platform
);

public record ShopUpsertDtos
{
    public long ShopId { get; set; }
    public long PartnerId { get; set; }
    public string? Name { get; set; }
    public string? Country { get; set; }
    public string? Platform { get; set; }
}
