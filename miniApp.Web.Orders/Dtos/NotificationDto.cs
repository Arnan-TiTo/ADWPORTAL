using System;

namespace miniApp.WebOrders.Dtos
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string? Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateNotificationDto
    {
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string? Type { get; set; }
    }

    public class UpdateNotificationDto
    {
        public bool IsRead { get; set; }
    }
}
