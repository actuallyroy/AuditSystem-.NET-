using System;
using System.Text.Json;

namespace AuditSystem.Domain.Entities
{
    public class Assignment
    {
        public Guid AssignmentId { get; set; }
        public Guid TemplateId { get; set; }
        public Guid AssignedToId { get; set; }
        public Guid AssignedById { get; set; }
        public Guid OrganisationId { get; set; }
        public JsonDocument StoreInfo { get; set; }
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public virtual Template Template { get; set; }
        public virtual User AssignedTo { get; set; }
        public virtual User AssignedBy { get; set; }
        public virtual Organisation Organisation { get; set; }
    }
} 