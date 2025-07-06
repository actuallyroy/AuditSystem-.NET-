using System;
using System.Collections.Generic;

namespace AuditSystem.Domain.Entities
{
    public class NotificationMessage
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public string Type { get; set; }
        public string Channel { get; set; }
        public Guid? UserId { get; set; }
        public Guid? OrganisationId { get; set; }
        public string RecipientEmail { get; set; }
        public string RecipientPhone { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Priority { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ScheduledFor { get; set; }
        public int RetryCount { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class EmailNotificationMessage : NotificationMessage
    {
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public string ReplyTo { get; set; }
        public List<string> Cc { get; set; } = new List<string>();
        public List<string> Bcc { get; set; } = new List<string>();
        public bool IsHtml { get; set; } = true;
        public List<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
    }

    public class SmsNotificationMessage : NotificationMessage
    {
        public string FromNumber { get; set; }
        public string ToNumber { get; set; }
        public bool IsUnicode { get; set; }
    }

    public class PushNotificationMessage : NotificationMessage
    {
        public string DeviceToken { get; set; }
        public string AppId { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public string Sound { get; set; }
        public int Badge { get; set; }
    }

    public class EmailAttachment
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
        public bool IsInline { get; set; }
        public string ContentId { get; set; }
    }

    public class NotificationQueueConfig
    {
        public string ExchangeName { get; set; } = "notifications";
        public string EmailQueueName { get; set; } = "email_notifications";
        public string SmsQueueName { get; set; } = "sms_notifications";
        public string PushQueueName { get; set; } = "push_notifications";
        public string DeadLetterQueueName { get; set; } = "notification_dlq";
        public string EmailRoutingKey { get; set; } = "email";
        public string SmsRoutingKey { get; set; } = "sms";
        public string PushRoutingKey { get; set; } = "push";
        public int MaxRetries { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 5000;
    }
} 