using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AuditSystem.Domain.Entities
{
    public class Template
    {
        public Guid TemplateId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public JsonDocument Questions { get; set; }
        public JsonDocument ScoringRules { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public Guid CreatedById { get; set; }
        public bool IsPublished { get; set; }
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual User CreatedBy { get; set; }
        public virtual ICollection<Assignment> Assignments { get; set; }
        public virtual ICollection<Audit> Audits { get; set; }
    }
} 