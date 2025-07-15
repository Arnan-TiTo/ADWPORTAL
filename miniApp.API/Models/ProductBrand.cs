using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace miniApp.API.Models
{
    public class ProductBrand
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public ICollection<Product>? Products { get; set; }
    }
}
