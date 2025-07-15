namespace miniApp.API.Models
{
    public class OrderDt
    {
        public int Id { get; set; }

        public int OrderHdId { get; set; }
        public OrderHd? OrderHd { get; set; }

        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Discount { get; set; }
        public decimal Total => (UnitPrice * Quantity) - Discount;
    }
}
