namespace adwportal.Dtos
{
    public record CompanyItemDtos(
        int Id, 
        string Name,
        int PartnerId
    );

    public record CompanyListItemDtos(
        int Id,
        string Name,
        int PartnerId,
        string PartnerName,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        int ShopCount
    );

    public class CompanyUpsertDtos
    {
        public string Name { get; set; } = "";
        public int PartnerId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public record MdwPartnerDtos(
        int Id, 
        string Name, 
        int PartnerId, 
        int? CompanysId,
        string? CompanyName
    );

    public record MdwShopDtos(int Id, long ShopId, string? Name, int PartnerId);

}
