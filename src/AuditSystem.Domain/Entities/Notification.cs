using System;
using System.Text.Json;

namespace AuditSystem.Domain.Entities
{
    public class Notification
    {
        public Guid NotificationId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? OrganisationId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Priority { get; set; } = "medium";
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public string Channel { get; set; } = "in_app";
        public string Status { get; set; } = "pending";
        public int RetryCount { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? ErrorMessage { get; set; }
        public JsonDocument? Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Organisation? Organisation { get; set; }
    }
} 