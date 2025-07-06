using System;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace AuditSystem.API.Models
{
    public class CreateAuditDto
    {
        [Required]
        public Guid TemplateId { get; set; }
        
        /// <summary>
        /// Assignment ID - if provided, the assignment will be converted to an audit
        /// </summary>
        public Guid? AssignmentId { get; set; }
        
        /// <summary>
        /// Store name
        /// </summary>
        public string? StoreName { get; set; }
        
        /// <summary>
        /// Store location/address
        /// </summary>
        public string? StoreLocation { get; set; }
        
        /// <summary>
        /// Store information as JSON object
        /// </summary>
        public JsonElement? StoreInfo { get; set; }
        
        /// <summary>
        /// Location information as JSON object (GPS coordinates)
        /// </summary>
        public JsonElement? Location { get; set; }
        
        /// <summary>
        /// Audit responses as JSON object matching the template questions
        /// Optional - if not provided, creates empty audit for later completion
        /// </summary>
        public JsonElement? Responses { get; set; }
        
        /// <summary>
        /// Media attachments as JSON object (photos, videos, signatures, etc.)
        /// </summary>
        public JsonElement? Media { get; set; }
        
        /// <summary>
        /// Audit status (draft, in_progress, submitted, completed)
        /// </summary>
        public string? Status { get; set; } = "draft";
        
        /// <summary>
        /// Pre-calculated score (optional - will be calculated if responses provided)
        /// </summary>
        public decimal? Score { get; set; }
        
        /// <summary>
        /// Number of critical issues identified
        /// </summary>
        public int? CriticalIssues { get; set; }
    }

    public class SubmitAuditDto
    {
        [Required]
        public Guid AuditId { get; set; }
        
        /// <summary>
        /// Audit responses as JSON object matching the template questions
        /// </summary>
        [Required]
        public JsonElement Responses { get; set; }
        
        /// <summary>
        /// Media attachments as JSON object (photos, videos, etc.)
        /// </summary>
        public JsonElement? Media { get; set; }
        
        /// <summary>
        /// Additional store information
        /// </summary>
        public JsonElement? StoreInfo { get; set; }
        
        /// <summary>
        /// Location information
        /// </summary>
        public JsonElement? Location { get; set; }
    }

    public class UpdateAuditStatusDto
    {
        [Required]
        public string? Status { get; set; }
        
        /// <summary>
        /// Manager notes for the audit
        /// </summary>
        public string? ManagerNotes { get; set; }
        
        /// <summary>
        /// Flag the audit for attention
        /// </summary>
        public bool? IsFlagged { get; set; }
    }

    public class AuditResponseDto
    {
        public Guid AuditId { get; set; }
        public Guid TemplateId { get; set; }
        public int? TemplateVersion { get; set; }
        public Guid AuditorId { get; set; }
        public Guid OrganisationId { get; set; }
        public string? Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// Store information as JSON object
        /// </summary>
        public JsonElement? StoreInfo { get; set; }
        
        /// <summary>
        /// Audit responses as JSON object
        /// </summary>
        public JsonElement? Responses { get; set; }
        
        /// <summary>
        /// Media attachments as JSON object
        /// </summary>
        public JsonElement? Media { get; set; }
        
        /// <summary>
        /// Location information as JSON object
        /// </summary>
        public JsonElement? Location { get; set; }
        
        public decimal? Score { get; set; }
        public int CriticalIssues { get; set; }
        public string? ManagerNotes { get; set; }
        public bool IsFlagged { get; set; }
        public bool SyncFlag { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties
        public string? TemplateName { get; set; }
        public string? AuditorName { get; set; }
        public string? OrganisationName { get; set; }
    }

    public class AuditSummaryDto
    {
        public Guid AuditId { get; set; }
        public Guid TemplateId { get; set; }
        public string? TemplateName { get; set; }
        public string? StoreId { get; set; }
        public string? StoreName { get; set; }
        public string? Address { get; set; }
        public Guid AuditorId { get; set; }
        public string? AuditorName { get; set; }
        public Guid OrganisationId { get; set; }
        public string? OrganisationName { get; set; }
        public string? Status { get; set; }
        public decimal? Score { get; set; }
        public int CriticalIssues { get; set; }
        public bool IsFlagged { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EndTime { get; set; }
        public string? RejectionReason { get; set; }
        public string? Notes { get; set; }
        public Guid? AssignmentId { get; set; }
    }
} 