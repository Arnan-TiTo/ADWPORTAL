using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace adwportal.Dtos
{
    /// <summary>
    /// FE DTO สำหรับแสดง Unified Order 1 รายการ
    /// ออกแบบให้รองรับได้ทั้งกรณีที่ API ส่งชื่อฟิลด์เป็น Items/Payments/Shipments/ShipTo
    /// หรือส่งเป็น ItemsJson/PaymentsJson/ShipmentsJson/ShipToJson (จาก SQL VIEW ตรง ๆ)
    /// โดย map มาลงที่ backing field เดียวกัน
    /// </summary>
    public sealed class FeUnifiedOrderDtos
    {
        // ========== คีย์หลัก + หัวตาราง ==========
        public long UnifiedOrderId { get; set; }
        public string? ExternalOrderNo { get; set; }
        public string? Channel { get; set; }
        public long ShopId { get; set; }
        public string? SellerId { get; set; }
        public string? BuyerUserId { get; set; }
        public string? BuyerUsername { get; set; }
        public string? OrderStatus { get; set; }

        public DateTime? CreatedTimeUtc { get; set; }
        public DateTime? UpdatedTimeUtc { get; set; }

        public decimal? EscrowAmount { get; set; }
        public decimal? BuyerPaidShippingFee { get; set; }
        public decimal? ActualShippingFee { get; set; }
        public decimal? PlatformShippingRebate { get; set; }
        public decimal? CommissionFee { get; set; }
        public decimal? ServiceFee { get; set; }
        public decimal? PlatformFee { get; set; }
        public decimal? PaymentTransactionFee { get; set; }
        public decimal? AmsCommissionFee { get; set; }
        public string? SellerVoucherCode { get; set; }

        // ========== ข้อมูลย่อย (เก็บเป็น JSON เพื่อง่ายและยืดหยุ่น) ==========

        // --- Items (array) ---
        private JsonElement _items;
        [JsonPropertyName("items")]
        public JsonElement Items { get => _items; set => _items = value; }

        // เผื่อ API ส่งชื่อ ItemsJson มา
        [JsonPropertyName("itemsJson")]
        public JsonElement ItemsJson { get => _items; set => _items = value; }

        // --- Payments (array) ---
        private JsonElement _payments;
        [JsonPropertyName("payments")]
        public JsonElement Payments { get => _payments; set => _payments = value; }

        [JsonPropertyName("paymentsJson")]
        public JsonElement PaymentsJson { get => _payments; set => _payments = value; }

        // --- Shipments (array) ---
        private JsonElement _shipments;
        [JsonPropertyName("shipments")]
        public JsonElement Shipments { get => _shipments; set => _shipments = value; }

        [JsonPropertyName("shipmentsJson")]
        public JsonElement ShipmentsJson { get => _shipments; set => _shipments = value; }

        // --- ShipTo (object) ---
        private JsonElement _shipTo;
        [JsonPropertyName("shipTo")]
        public JsonElement ShipTo { get => _shipTo; set => _shipTo = value; }

        [JsonPropertyName("shipToJson")]
        public JsonElement ShipToJson { get => _shipTo; set => _shipTo = value; }
    }
}
