namespace miniApp.WebOrders.Models
{
    public class LocationDto
    {
        public string Name { get; set; } = string.Empty;
        public float Latitude { get; set; } = 0; 
        public float Longitude { get; set; } = 0;
        public string? Note { get; set; } = string.Empty;
        public IFormFile? Image { get; set; }
    }
}
