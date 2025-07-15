using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace miniApp.API.Models
{
    public class ProductCategory
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public ICollection<Product>? Products { get; set; }
    }
}
