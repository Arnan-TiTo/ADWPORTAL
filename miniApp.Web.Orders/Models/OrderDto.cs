using miniApp.WebOrders.Dtos;

namespace miniApp.WebOrders.Models
{
    public class OrderDto
    {

        public string? OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = "";
        public string Gender { get; set; } = "";
        public DateTime? BirthDate { get; set; }
        public string Occupation { get; set; } = "";
        public string Nationality { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string AddressLine { get; set; } = "";
        public string SubDistrict { get; set; } = "";
        public string District { get; set; } = "";
        public string Province { get; set; } = "";
        public string ZipCode { get; set; } = "";
        public bool MayIAsk { get; set; }
        public string PaymentMethod { get; set; } = "";
        public string? SlipImage { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int LocationId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
    }

    public class OrderCreateDto
    {
        public string CustomerName { get; set; } = "";
        public string Gender { get; set; } = "";
        public DateTime? BirthDate { get; set; }
        public string Occupation { get; set; } = "";
        public string Nationality { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string AddressLine { get; set; } = "";
        public string SubDistrict { get; set; } = "";
        public string District { get; set; } = "";
        public string Province { get; set; } = "";
        public string ZipCode { get; set; } = "";
        public string Social { get; set; } = "";
        public bool MayIAsk { get; set; }
        public string PaymentMethod { get; set; } = "";
        public string? SlipImage { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderHistoryViewDto
    {
        public int Id { get; set; }
        public string? OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? AddressLine { get; set; }
        public string? SubDistrict { get; set; }
        public string? District { get; set; }
        public string? Province { get; set; }
        public string? ZipCode { get; set; }
        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Occupation { get; set; }
        public string? Nationality { get; set; }
        public bool MayIAsk { get; set; }
        public string? PaymentMethod { get; set; }
        public string? SlipImage { get; set; }
        public List<OrderItemHistoryDto> Items { get; set; } = new();
    }

    public class OrderItemHistoryDto
    {
        public int LocationId { get; set; }
        public int ProductId { get; set; }
        public string? Sku { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public string? ImageUrl { get; set; }
    }

}
