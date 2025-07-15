namespace miniApp.WebOrders.Models
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public string BrandName { get; set; } = "";
    }
}
