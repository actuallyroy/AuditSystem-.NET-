using AuditSystem.Domain.Repositories;
using AuditSystem.Domain.Services;
using AuditSystem.Services.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public interface IDashboardService
    {
        Task<DashboardResponse> GetDashboardDataAsync(Guid? organizationId = null);
        Task<DashboardResponse> GetDashboardDataForUserAsync(Guid userId, string userRole, Guid? organizationId = null);
    }

    public class DashboardService : IDashboardService
    {
        private readonly IAuditRepository _auditRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITemplateRepository _templateRepository;
        private readonly IOrganisationRepository _organisationRepository;
        private readonly DashboardCacheService _dashboardCacheService;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IAuditRepository auditRepository,
            IAssignmentRepository assignmentRepository,
            IUserRepository userRepository,
            ITemplateRepository templateRepository,
            IOrganisationRepository organisationRepository,
            DashboardCacheService dashboardCacheService,
            ILogger<DashboardService> logger)
        {
            _auditRepository = auditRepository;
            _assignmentRepository = assignmentRepository;
            _userRepository = userRepository;
            _templateRepository = templateRepository;
            _organisationRepository = organisationRepository;
            _dashboardCacheService = dashboardCacheService;
            _logger = logger;
        }

        public async Task<DashboardResponse> GetDashboardDataAsync(Guid? organizationId = null)
        {
            try
            {
                // Try to get from cache first
                var cachedData = await _dashboardCacheService.GetDashboardMetricsAsync<DashboardResponse>(organizationId);
                if (cachedData != null)
                {
                    _logger.LogDebug("Dashboard data retrieved from cache for organization {OrganizationId}", organizationId);
                    return cachedData;
                }

                // Generate fresh data
                var dashboardData = await GenerateDashboardDataAsync(organizationId);
                
                // Cache the result
                await _dashboardCacheService.SetDashboardMetricsAsync(dashboardData, organizationId);
                
                _logger.LogDebug("Dashboard data generated and cached for organization {OrganizationId}", organizationId);
                return dashboardData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dashboard data for organization {OrganizationId}", organizationId);
                throw;
            }
        }

        public async Task<DashboardResponse> GetDashboardDataForUserAsync(Guid userId, string userRole, Guid? organizationId = null)
        {
            try
            {
                // For now, return the same data for all users
                // In a real implementation, you might filter data based on user role and permissions
                return await GetDashboardDataAsync(organizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dashboard data for user {UserId} with role {UserRole}", userId, userRole);
                throw;
            }
        }

        private async Task<DashboardResponse> GenerateDashboardDataAsync(Guid? organizationId)
        {
            var dashboardData = new DashboardResponse();

            // Get all audits (filtered by organization if specified)
            var allAudits = organizationId.HasValue 
                ? await _auditRepository.GetAuditsByOrganisationIdAsync(organizationId.Value)
                : await _auditRepository.GetAllAsync();

            // Get all assignments (filtered by organization if specified)
            var allAssignments = organizationId.HasValue
                ? await _assignmentRepository.GetAssignmentsByOrganisationAsync(organizationId.Value)
                : await _assignmentRepository.GetAllAsync();

            // Get all users (filtered by organization if specified)
            var allUsers = organizationId.HasValue
                ? await _userRepository.GetUsersByOrganisationAsync(organizationId.Value)
                : await _userRepository.GetAllAsync();

            // Calculate stats
            dashboardData.Stats = await CalculateStatsAsync(allAudits, allAssignments, allUsers);

            // Get recent audits
            dashboardData.RecentAudits = await GetRecentAuditsAsync(allAudits);

            // Get upcoming assignments
            dashboardData.UpcomingAssignments = await GetUpcomingAssignmentsAsync(allAssignments);

            // Get regional data (mock data for now)
            dashboardData.RegionalData = GetRegionalData();

            return dashboardData;
        }

        private async Task<DashboardStats> CalculateStatsAsync(
            IEnumerable<Domain.Entities.Audit> audits,
            IEnumerable<Domain.Entities.Assignment> assignments,
            IEnumerable<Domain.Entities.User> users)
        {
            var stats = new DashboardStats();

            // Total Audits
            stats.TotalAudits = audits.Count();

            // Completion Rate
            var completedAudits = audits.Count(a => a.Status == "completed" || a.Status == "submitted");
            stats.CompletionRate = stats.TotalAudits > 0 ? (double)completedAudits / stats.TotalAudits * 100 : 0;

            // Critical Issues
            stats.CriticalIssues = audits.Sum(a => a.CriticalIssues);

            // Active Auditors (users who have completed audits in the last 30 days)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var activeAuditorIds = audits
                .Where(a => a.EndTime >= thirtyDaysAgo)
                .Select(a => a.AuditorId)
                .Distinct();
            stats.ActiveAuditors = activeAuditorIds.Count();

            return stats;
        }

        private async Task<List<RecentAuditDto>> GetRecentAuditsAsync(IEnumerable<Domain.Entities.Audit> audits)
        {
            var recentAudits = audits
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(async audit =>
                {
                    var auditor = await _userRepository.GetByIdAsync(audit.AuditorId);
                    var template = await _templateRepository.GetByIdAsync(audit.TemplateId);

                    return new RecentAuditDto
                    {
                        Id = $"AUD-{audit.AuditId.ToString().Substring(0, 8).ToUpper()}",
                        Store = ExtractStoreName(audit.StoreInfo),
                        Auditor = auditor?.Username ?? "Unknown",
                        Status = FormatStatus(audit.Status),
                        Score = audit.Score,
                        Date = audit.CreatedAt.ToString("yyyy-MM-dd")
                    };
                });

            var results = await Task.WhenAll(recentAudits);
            return results.ToList();
        }

        private async Task<List<UpcomingAssignmentDto>> GetUpcomingAssignmentsAsync(IEnumerable<Domain.Entities.Assignment> assignments)
        {
            var upcomingAssignments = assignments
                .Where(a => a.Status == "pending" && a.DueDate > DateTime.UtcNow)
                .OrderBy(a => a.DueDate)
                .Take(5)
                .Select(async assignment =>
                {
                    var auditor = await _userRepository.GetByIdAsync(assignment.AssignedToId);
                    var template = await _templateRepository.GetByIdAsync(assignment.TemplateId);

                    return new UpcomingAssignmentDto
                    {
                        Id = $"ASG-{assignment.AssignmentId.ToString().Substring(0, 8).ToUpper()}",
                        Template = template?.Name ?? "Unknown Template",
                        Auditor = auditor?.Username ?? "Unknown",
                        Store = ExtractStoreName(assignment.StoreInfo),
                        DueDate = assignment.DueDate?.ToString("yyyy-MM-dd") ?? "No due date",
                        Priority = assignment.Priority ?? "Medium"
                    };
                });

            var results = await Task.WhenAll(upcomingAssignments);
            return results.ToList();
        }

        private List<RegionalDataDto> GetRegionalData()
        {
            // Mock regional data - in a real implementation, this would come from the database
            return new List<RegionalDataDto>
            {
                new RegionalDataDto { Region = "North Delhi", Completed = 85, Total = 100 },
                new RegionalDataDto { Region = "South Delhi", Completed = 92, Total = 110 },
                new RegionalDataDto { Region = "East Delhi", Completed = 78, Total = 95 },
                new RegionalDataDto { Region = "West Delhi", Completed = 88, Total = 105 }
            };
        }

        private string ExtractStoreName(System.Text.Json.JsonDocument? storeInfo)
        {
            if (storeInfo == null) return "Unknown Store";

            try
            {
                var storeName = storeInfo.RootElement.GetProperty("storeName").GetString();
                return storeName ?? "Unknown Store";
            }
            catch
            {
                return "Unknown Store";
            }
        }

        private string FormatStatus(string status)
        {
            return status?.ToLower() switch
            {
                "completed" or "submitted" => "Completed",
                "in_progress" => "In Progress",
                "flagged" => "Flagged",
                _ => status ?? "Unknown"
            };
        }
    }
} 