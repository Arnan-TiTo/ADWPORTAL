using System;
using System.Collections.Generic;

namespace miniApp.API.Models
{
    public class OrderHd
    {
        public int Id { get; set; }

        public string OrderNo { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        // Customer Info
        public string CustomerName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty; // เช่น Male, Female, Other
        public DateTime? BirthDate { get; set; }
        public string Occupation { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;

        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;

        public string AddressLine { get; set; } = string.Empty;
        public string SubDistrict { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        
        // Consent Checkbox
        public bool MayIAsk { get; set; } = false;

        // Payment Slip
        public string? PaymentSlip { get; set; }

        // Order Items
        public List<OrderDt> OrderDts { get; set; } = new();
    }

}
