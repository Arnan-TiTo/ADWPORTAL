namespace miniApp.API.Dtos
{
    public class ProductBrandDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
    }

    public class CreateProductBrandDto
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
    }

    public class UpdateProductBrandDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
    }
}
