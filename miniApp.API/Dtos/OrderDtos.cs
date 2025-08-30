using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace miniApp.API.Dtos
{
    public class OrderCreateDto
    {
        [Required]
        public string CustomerName { get; set; } = string.Empty;
        [Required]
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string SubDistrict { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string? Social { get; set; }
        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Occupation { get; set; }
        public string? Nationality { get; set; }
        public bool MayIAsk { get; set; } = false;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? SlipImage { get; set; }
        public int CreatedByUserId { get; set; } 

        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderUpdateDto : OrderCreateDto
    {
        [Required]
        public int Id { get; set; }

    }
    
    public class OrderViewDto
    {
        public int Id { get; set; }
        public string OrderNo { get; set; } = "";
        public DateTime OrderDate { get; set; }

        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string AddressLine { get; set; } = "";
        public string SubDistrict { get; set; } = "";
        public string District { get; set; } = "";
        public string Province { get; set; } = "";
        public string ZipCode { get; set; } = "";
        public string? Social { get; set; }

        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Occupation { get; set; }
        public string? Nationality { get; set; }

        public bool MayIAsk { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;
        public string? SlipImage { get; set; }
        public int? CreatedByUserId { get; set; }

        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int LocationId { get; set; }
        public int ProductId { get; set; }
        public string Sku { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Total => Quantity * UnitPrice;
        public string ImageUrl { get; set; } = "";
    }
}
