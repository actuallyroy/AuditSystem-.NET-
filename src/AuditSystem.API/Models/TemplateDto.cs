using System;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace AuditSystem.API.Models
{
    public class CreateTemplateDto
    {
        [Required]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        [Required]
        public string Category { get; set; }
        
        /// <summary>
        /// Template questions as JSON object. Example: [{"id":"q1","type":"rating","text":"How clean are floors?","scale":{"min":1,"max":5},"required":true}]
        /// </summary>
        public JsonElement? Questions { get; set; }
        
        /// <summary>
        /// Scoring rules as JSON object. Example: {"totalScore":{"calculation":"weighted_average","weights":{"q1":0.5}},"passingScore":3.0}
        /// </summary>
        public JsonElement? ScoringRules { get; set; }
        
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }
    
    public class UpdateTemplateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        
        /// <summary>
        /// Template questions as JSON object. Example: [{"id":"q1","type":"rating","text":"How clean are floors?","scale":{"min":1,"max":5},"required":true}]
        /// </summary>
        public JsonElement? Questions { get; set; }
        
        /// <summary>
        /// Scoring rules as JSON object. Example: {"totalScore":{"calculation":"weighted_average","weights":{"q1":0.5}},"passingScore":3.0}
        /// </summary>
        public JsonElement? ScoringRules { get; set; }
        
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }
    
    public class TemplateResponseDto
    {
        public Guid TemplateId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        
        /// <summary>
        /// Template questions as JSON object
        /// </summary>
        public JsonElement? Questions { get; set; }
        
        /// <summary>
        /// Scoring rules as JSON object
        /// </summary>
        public JsonElement? ScoringRules { get; set; }
        
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public Guid CreatedById { get; set; }
        public bool IsPublished { get; set; }
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 