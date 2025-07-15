namespace miniApp.API.Dtos
{
    public class ProductCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class CreateProductCategoryDto
    {
        public string Name { get; set; } = "";
    }

    public class UpdateProductCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
