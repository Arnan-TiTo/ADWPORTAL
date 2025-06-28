using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace miniApp.API.Dtos
{
    public class ProductDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Sku { get; set; }

        public int Quantity { get; set; }

        public string? Note { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int LocationId { get; set; }

        public IFormFile? Image { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class ProductUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Sku { get; set; }

        public int Quantity { get; set; }

        public string? Note { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int LocationId { get; set; }

        public IFormFile? Image { get; set; } // รองรับการแก้ไขรูปภาพ
    }

    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Sku { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
        public string? ImageUrl { get; set; }
        public int UserId { get; set; }
        public string UserFullname { get; set; } = "";
        public int LocationId { get; set; }
        public string LocationName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}