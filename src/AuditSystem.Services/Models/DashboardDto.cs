using System;
using System.Collections.Generic;

namespace AuditSystem.Services.Models
{
    public class DashboardResponse
    {
        public DashboardStats Stats { get; set; } = new();
        public List<RecentAuditDto> RecentAudits { get; set; } = new();
        public List<UpcomingAssignmentDto> UpcomingAssignments { get; set; } = new();
        public List<RegionalDataDto> RegionalData { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class DashboardStats
    {
        public int TotalAudits { get; set; }
        public double CompletionRate { get; set; }
        public int CriticalIssues { get; set; }
        public int ActiveAuditors { get; set; }
        public string TotalAuditsChange { get; set; } = "+12% from last month";
        public string CompletionRateChange { get; set; } = "+3% from last week";
        public string CriticalIssuesChange { get; set; } = "-5 from yesterday";
        public string ActiveAuditorsChange { get; set; } = "+2 new this week";
    }

    public class RecentAuditDto
    {
        public string Id { get; set; } = string.Empty;
        public string Store { get; set; } = string.Empty;
        public string Auditor { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal? Score { get; set; }
        public string Date { get; set; } = string.Empty;
    }

    public class UpcomingAssignmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public string Auditor { get; set; } = string.Empty;
        public string Store { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
    }

    public class RegionalDataDto
    {
        public string Region { get; set; } = string.Empty;
        public int Completed { get; set; }
        public int Total { get; set; }
        public double CompletionPercentage => Total > 0 ? (double)Completed / Total * 100 : 0;
    }
} 