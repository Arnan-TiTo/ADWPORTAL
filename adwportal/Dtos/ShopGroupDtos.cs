namespace adwportal.Dtos;

// ===== Shop Group =====
public record ShopGroupDtos(
    int Id,
    string GroupCode,
    string Name,
    string? Description,
    string Permission,      // read | write | admin
    bool IsActive,
    DateTime CreatedAt,
    int UserCount,
    int ShopCount
);

public record ShopGroupDetailDtos(
    int Id,
    string GroupCode,
    string Name,
    string? Description,
    string Permission,
    bool IsActive,
    DateTime CreatedAt,
    List<ShopGroupUserDtos> Users,
    List<ShopGroupShopDtos> Shops
);

public record ShopGroupUserDtos(int Id, int UserId, string? Username, DateTime CreatedAt);
public record ShopGroupShopDtos(int Id, long ShopId, string? ShopName, string? Platform, DateTime CreatedAt);

public class ShopGroupUpsertDtos
{
    public string GroupCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Permission { get; set; } = "read";
    public bool IsActive { get; set; } = true;
}

// ===== User-centric query result =====
public record UserEffectiveShopDtos(
    long ShopId,
    string? ShopName,
    string? Platform,
    int PartnerId,
    string Permission,       // highest permission across all groups
    string[] Groups          // group codes
);

// ===== User's group membership (for UserEdit display) =====
public record UserGroupInfoDtos(
    int Id,
    string GroupCode,
    string Name,
    string Permission,
    bool IsActive,
    int ShopCount
);
