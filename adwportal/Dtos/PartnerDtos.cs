namespace adwportal.Dtos
{
    public class PartnerDtos
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public long PartnerId { get; set; }
        public string? PartnerKey { get; set; }
        public string? Environment { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CompanysId { get; set; }
        public string? CompanyName { get; set; }
    }

    public class PartnerUpsertDtos
    {
        public string Name { get; set; } = "";
        public long PartnerId { get; set; }
        public string? PartnerKey { get; set; } = "";
        public string? Environment { get; set; }
        public int? CompanysId { get; set; }
    }
}
