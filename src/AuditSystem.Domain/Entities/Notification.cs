using System;
using System.Collections.Generic;

namespace AuditSystem.Domain.Entities
{
    public class Notification
    {
        public Guid NotificationId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? OrganisationId { get; set; }
        public string Type { get; set; } // 'assignment', 'audit_completed', 'audit_approved', 'audit_rejected', 'system'
        public string Title { get; set; }
        public string Message { get; set; }
        public string Priority { get; set; } // 'low', 'medium', 'high', 'urgent'
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public string Channel { get; set; } // 'email', 'sms', 'push', 'in_app'
        public string Status { get; set; } // 'pending', 'sent', 'failed', 'delivered'
        public int RetryCount { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Organisation Organisation { get; set; }
    }

    public class NotificationTemplate
    {
        public Guid TemplateId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Channel { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, string> Placeholders { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
} 