using System;
using System.Text.Json;

namespace AuditSystem.Domain.Entities
{
    public class Report
    {
        public Guid ReportId { get; set; }
        public Guid GeneratedById { get; set; }
        public string Name { get; set; }
        public string ReportType { get; set; }
        public string Format { get; set; }
        public JsonDocument FiltersApplied { get; set; }
        public string FileUrl { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Schedule { get; set; }

        // Navigation properties
        public virtual User GeneratedBy { get; set; }
    }
} 