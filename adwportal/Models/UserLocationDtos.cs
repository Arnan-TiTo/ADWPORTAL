namespace adwportal.Models
{
    public class LocationOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class UserLocationDtos
    {
        public int UserId { get; set; }
        public int LocationId { get; set; }
    }

    public class UserLocationView
    {
        public int UserId { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; } = "";
        public bool IsEditing { get; set; } = false;
        public int EditLocationId { get; set; }
    }

    public class UpdateUserLocationsRequest
    {
        public int[] LocationIds { get; set; } = Array.Empty<int>();
    }
}
