using System;
using System.Text.Json;

namespace AuditSystem.Domain.Entities
{
    public class Log
    {
        public Guid LogId { get; set; }
        public Guid? UserId { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public JsonDocument Metadata { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime LoggedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
    }
} 