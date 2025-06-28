using System;
using System.Text.Json;

namespace AuditSystem.API.Models
{
    public class CreateTemplateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Questions { get; set; } // JSON string
        public string ScoringRules { get; set; } // JSON string
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }
    
    public class UpdateTemplateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Questions { get; set; } // JSON string
        public string ScoringRules { get; set; } // JSON string
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }
    
    public class TemplateResponseDto
    {
        public Guid TemplateId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Questions { get; set; } // JSON string instead of JsonDocument
        public string ScoringRules { get; set; } // JSON string instead of JsonDocument
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public Guid CreatedById { get; set; }
        public bool IsPublished { get; set; }
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 