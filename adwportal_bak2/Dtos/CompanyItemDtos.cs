namespace adwportal.Dtos
{
    public record CompanyItemDtos(
        int Id, 
        string Name,
        int? PartnerId
    );

    public class CompanyDtos
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public record CompanyListItemDtos(
        int Id,
        string Name,
        int? PartnerId,
        string? PartnerName,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int ShopCount
    );

    public class CompanyUpsertBasicDtos
    {
        public string Name { get; set; } = ""; 
        public int? PartnerId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CompanyUpsertDtos
    {
        public string Name { get; set; } = "";
        public int? PartnerId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public record MdwPartnerDtos(
        int Id,
        string? Name,
        long? PartnerId, 
        int? CompanysId,
        string? CompanyName
    );

    public record MdwShopDtos(
        int Id, 
        int ShopId, 
        string? Name, 
        long? PartnerId, 
        string? Platform
    );

}
