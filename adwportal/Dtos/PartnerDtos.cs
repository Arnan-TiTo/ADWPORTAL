namespace adwportal.Dtos
{
    public record PartnerDtos(
        int Id,
        string? Name,
        int PartnerId,
        string PartnerKey,
        string? Environment,
        DateTime CreatedAt
    );

    public class PartnerUpsertDtos
    {
        public string Name { get; set; } = "";
        public int PartnerId { get; set; }
        public string PartnerKey { get; set; } = "";
        public string? Environment { get; set; }
    }
}
