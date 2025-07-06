using System;
using System.Text.Json;

namespace AuditSystem.Domain.Entities
{
    public class Audit
    {
        public Guid AuditId { get; set; }
        public Guid TemplateId { get; set; }
        public int? TemplateVersion { get; set; }
        public Guid AuditorId { get; set; }
        public Guid OrganisationId { get; set; }
        public string Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public JsonDocument? StoreInfo { get; set; }
        public required JsonDocument Responses { get; set; }
        public JsonDocument? Media { get; set; }
        public JsonDocument? Location { get; set; }
        public decimal? Score { get; set; }
        public int CriticalIssues { get; set; }
        public string? ManagerNotes { get; set; }
        public bool IsFlagged { get; set; }
        public bool SyncFlag { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual Template? Template { get; set; }
        public virtual User? Auditor { get; set; }
        public virtual Organisation? Organisation { get; set; }
    }
} 