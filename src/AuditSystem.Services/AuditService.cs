using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Repositories;
using AuditSystem.Infrastructure.Repositories;
using AuditSystem.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AuditSystem.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository _auditRepository;
        private readonly ITemplateRepository _templateRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IOrganisationRepository _organisationRepository;

        public AuditService(
            IAuditRepository auditRepository,
            ITemplateRepository templateRepository,
            IAssignmentRepository assignmentRepository,
            IUserRepository userRepository,
            IOrganisationRepository organisationRepository)
        {
            _auditRepository = auditRepository;
            _templateRepository = templateRepository;
            _assignmentRepository = assignmentRepository;
            _userRepository = userRepository;
            _organisationRepository = organisationRepository;
        }

        public async Task<Audit> GetAuditByIdAsync(Guid auditId)
        {
            return await _auditRepository.GetByIdAsync(auditId);
        }

        public async Task<Audit?> GetAuditByAssignmentAsync(Guid assignmentId)
        {
            return await _auditRepository.GetAuditByAssignmentIdAsync(assignmentId);
        }

        public async Task<IEnumerable<Audit>> GetAllAuditsAsync()
        {
            return await _auditRepository.GetAllAuditsWithNavigationPropertiesAsync();
        }

        public async Task<IEnumerable<Audit>> GetAuditsByAuditorAsync(Guid auditorId)
        {
            return await _auditRepository.GetAuditsByAuditorIdAsync(auditorId);
        }

        public async Task<IEnumerable<Audit>> GetAuditsByOrganisationAsync(Guid organisationId)
        {
            return await _auditRepository.GetAuditsByOrganisationIdAsync(organisationId);
        }

        public async Task<IEnumerable<Audit>> GetAuditsByTemplateAsync(Guid templateId)
        {
            return await _auditRepository.GetAuditsByTemplateIdAsync(templateId);
        }

        public async Task<IEnumerable<Audit>> GetAuditsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _auditRepository.GetAuditsByDateRangeAsync(startDate, endDate);
        }

        public async Task<Audit> StartAuditAsync(Guid templateId, Guid auditorId, Guid organisationId, Guid assignmentId)
        {
            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template == null)
                throw new ArgumentException("Template not found");

            var auditor = await _userRepository.GetByIdAsync(auditorId);
            if (auditor == null)
                throw new ArgumentException("Auditor not found");

            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null)
                throw new ArgumentException("Assignment not found");

            // Temporarily comment out organization validation to test
            // var organisation = await _organisationRepository.GetByIdAsync(organisationId);
            // if (organisation == null)
            //     throw new ArgumentException("Organisation not found");

            var audit = new Audit
            {
                AuditId = Guid.NewGuid(),
                TemplateId = templateId,
                TemplateVersion = template?.Version,
                AuditorId = auditorId,
                OrganisationId = organisationId,
                AssignmentId = assignmentId,
                Status = "in_progress",
                StartTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                SyncFlag = false,
                IsFlagged = false,
                CriticalIssues = 0,
                StoreInfo = null,
                Responses = null,
                Media = null,
                Location = null,
                Score = null,
                ManagerNotes = null
            };

            // Ensure all DateTime values are UTC before saving
            EnsureAuditDateTimesAreUtc(audit);
            
            await _auditRepository.AddAsync(audit);
            await _auditRepository.SaveChangesAsync();
            return audit;
        }

        public async Task<Audit> StartAuditFromAssignmentAsync(Guid templateId, Guid auditorId, Guid organisationId, Guid assignmentId, JsonDocument? storeInfo = null, JsonDocument? location = null)
        {
            try
            {
                // For now, skip assignment validation to avoid DateTime tracking issues
                // TODO: Implement proper assignment validation without Entity Framework tracking issues
                
                // Create the audit directly instead of calling StartAuditAsync to avoid double saving
                var template = await _templateRepository.GetByIdAsync(templateId);
                if (template == null)
                    throw new ArgumentException("Template not found");

                var auditor = await _userRepository.GetByIdAsync(auditorId);
                if (auditor == null)
                    throw new ArgumentException("Auditor not found");

                // Ensure all DateTime fields in loaded entities are UTC
                EnsureTemplateDateTimesAreUtc(template);
                EnsureUserDateTimesAreUtc(auditor);

                var audit = new Audit
                {
                    AuditId = Guid.NewGuid(),
                    TemplateId = templateId,
                    TemplateVersion = template?.Version,
                    AuditorId = auditorId,
                    OrganisationId = organisationId,
                    AssignmentId = assignmentId,
                    Status = "in_progress",
                    // StartTime = DateTime.Parse("2025-07-05 16:19:51.39876+00"), // Ignore this for now
                    // CreatedAt = DateTime.Parse("2025-07-05 16:19:51.39876+00"), // Ignore this for now
                    SyncFlag = false,
                    IsFlagged = false,
                    CriticalIssues = 0,
                    StoreInfo = storeInfo,
                    Responses = JsonDocument.Parse("{}"),
                    Media = null,
                    Location = location,
                    Score = null,
                    ManagerNotes = null
                };

                
                await _auditRepository.AddAsync(audit);
                await _auditRepository.SaveChangesAsync();
                return audit;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error in StartAuditFromAssignmentAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<Audit> SubmitAuditAsync(Audit audit)
        {
            if (audit == null)
                throw new ArgumentNullException(nameof(audit));

            // Validate audit exists
            var existingAudit = await _auditRepository.GetByIdAsync(audit.AuditId);
            if (existingAudit == null)
                throw new ArgumentException("Audit not found");

            // Update audit with submitted data
            existingAudit.Responses = audit.Responses;
            existingAudit.Media = audit.Media;
            existingAudit.StoreInfo = audit.StoreInfo ?? existingAudit.StoreInfo;
            existingAudit.Location = audit.Location ?? existingAudit.Location;
            existingAudit.EndTime = DateTime.UtcNow;
            
            // Only set status to "submitted" if it was previously "in_progress" (draft audit)
            // Otherwise, respect the existing status that was set by the controller
            if (existingAudit.Status?.ToLower() == "in_progress")
            {
                existingAudit.Status = "submitted";
            }
            
            existingAudit.SyncFlag = true;

            // Calculate score if responses are provided
            if (audit.Responses != null)
            {
                existingAudit.Score = await CalculateAuditScoreAsync(existingAudit);
            }

            // Ensure all DateTime values are UTC before saving
            EnsureAuditDateTimesAreUtc(existingAudit);
            
            _auditRepository.Update(existingAudit);
            await _auditRepository.SaveChangesAsync();
            return existingAudit;
        }

        public async Task<bool> SyncAuditAsync(Guid auditId)
        {
            var audit = await _auditRepository.GetByIdAsync(auditId);
            if (audit == null)
                return false;

            audit.SyncFlag = false;
            
            // Ensure all DateTime values are UTC before saving
            EnsureAuditDateTimesAreUtc(audit);
            
            _auditRepository.Update(audit);
            await _auditRepository.SaveChangesAsync();
            return true;
        }

        public async Task<Audit> UpdateAuditStatusAsync(Guid auditId, string status)
        {
            var audit = await _auditRepository.GetByIdAsync(auditId);
            if (audit == null)
                throw new ArgumentException("Audit not found");

            // Validate status
            var validStatuses = new[] { "in_progress", "submitted", "approved", "rejected", "pending_review" };
            if (!validStatuses.Contains(status.ToLower()))
                throw new ArgumentException("Invalid audit status");

            audit.Status = status.ToLower();
            
            // Ensure all DateTime values are UTC before saving
            EnsureAuditDateTimesAreUtc(audit);
            
            _auditRepository.Update(audit);
            await _auditRepository.SaveChangesAsync();
            return audit;
        }

        public async Task<Audit> FlagAuditAsync(Guid auditId, bool isFlagged)
        {
            var audit = await _auditRepository.GetByIdAsync(auditId);
            if (audit == null)
                throw new ArgumentException("Audit not found");

            audit.IsFlagged = isFlagged;
            
            // Ensure all DateTime values are UTC before saving
            EnsureAuditDateTimesAreUtc(audit);
            
            _auditRepository.Update(audit);
            await _auditRepository.SaveChangesAsync();
            return audit;
        }

        public async Task<Audit> AddManagerNotesAsync(Guid auditId, string notes)
        {
            var audit = await _auditRepository.GetByIdAsync(auditId);
            if (audit == null)
                throw new ArgumentException("Audit not found");

            audit.ManagerNotes = notes;
            
            // Ensure all DateTime values are UTC before saving
            EnsureAuditDateTimesAreUtc(audit);
            
            _auditRepository.Update(audit);
            await _auditRepository.SaveChangesAsync();
            return audit;
        }

        public async Task<Audit> RecalculateAuditScoreAsync(Guid auditId)
        {
            var audit = await _auditRepository.GetByIdAsync(auditId);
            if (audit == null)
                throw new ArgumentException("Audit not found");

            // Recalculate score
            audit.Score = await CalculateAuditScoreAsync(audit);
            
            // Ensure all DateTime values are UTC before saving
            EnsureAuditDateTimesAreUtc(audit);
            
            _auditRepository.Update(audit);
            await _auditRepository.SaveChangesAsync();
            return audit;
        }

        public async Task<Audit> UpdateAuditAsync(Audit audit)
        {
            if (audit == null)
                throw new ArgumentNullException(nameof(audit));

            // Validate audit exists
            var existingAudit = await _auditRepository.GetByIdAsync(audit.AuditId);
            if (existingAudit == null)
                throw new ArgumentException("Audit not found");

            // Update audit with provided data
            if (audit.StoreInfo != null)
                existingAudit.StoreInfo = audit.StoreInfo;
            if (audit.Location != null)
                existingAudit.Location = audit.Location;
            if (audit.Media != null)
                existingAudit.Media = audit.Media;
            if (!string.IsNullOrEmpty(audit.Status))
                existingAudit.Status = audit.Status;
            existingAudit.CriticalIssues = audit.CriticalIssues;
            if (audit.Score.HasValue)
                existingAudit.Score = audit.Score.Value;
            
            // Ensure all DateTime values are UTC before saving
            EnsureAuditDateTimesAreUtc(existingAudit);
            
            _auditRepository.Update(existingAudit);
            await _auditRepository.SaveChangesAsync();
            return existingAudit;
        }

        public async Task<decimal> CalculateAuditScoreAsync(Audit audit)
        {
            if (audit == null || audit.Responses == null)
                return 0;

            try
            {
                // Get the template to understand scoring rules
                var template = await _templateRepository.GetByIdAsync(audit.TemplateId);
                if (template == null || template.ScoringRules == null)
                    return 0;

                var responses = audit.Responses.RootElement;
                var scoringRules = template.ScoringRules.RootElement;

                decimal totalScore = 0;
                int criticalIssues = 0;

                // Parse scoring rules
                if (scoringRules.TryGetProperty("questionScores", out var questionScores) && 
                    questionScores.ValueKind == JsonValueKind.Object)
                {
                    // Iterate through each question in the scoring rules
                    foreach (var questionScore in questionScores.EnumerateObject())
                    {
                        var questionId = questionScore.Name;
                        var maxScore = questionScore.Value.GetDecimal();

                        // Check if this question was answered
                        if (responses.TryGetProperty(questionId, out var response))
                        {
                            var score = CalculateQuestionScore(response, maxScore);
                            totalScore += score;

                            // Check for critical issues (scores below 50% of max score)
                            if (score < (maxScore * 0.5m))
                            {
                                criticalIssues++;
                            }
                        }
                    }
                }

                // Update critical issues count
                audit.CriticalIssues = criticalIssues;

                return Math.Round(totalScore, 2);
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error calculating audit score: {ex.Message}");
                return 0;
            }
        }

        private decimal CalculateQuestionScore(JsonElement response, decimal maxScore)
        {
            try
            {
                // Handle different response types
                switch (response.ValueKind)
                {
                    case JsonValueKind.String:
                        var answer = response.GetString()?.ToLower();
                        if (string.IsNullOrEmpty(answer))
                            return 0;

                        // Score based on answer content
                        if (answer.Contains("yes") || answer.Contains("excellent") || answer.Contains("good"))
                            return maxScore;
                        else if (answer.Contains("partially") || answer.Contains("fair"))
                            return maxScore * 0.6m;
                        else if (answer.Contains("no") || answer.Contains("poor"))
                            return 0;
                        else
                            return maxScore * 0.5m; // Default score for other answers

                    case JsonValueKind.Number:
                        var numericValue = response.GetDecimal();
                        // If it's already a score, return it (capped at max score)
                        return Math.Min(numericValue, maxScore);

                    case JsonValueKind.Object:
                        // For complex responses, check if there's a score property
                        if (response.TryGetProperty("score", out var scoreElement) && 
                            scoreElement.ValueKind == JsonValueKind.Number)
                        {
                            var score = scoreElement.GetDecimal();
                            return Math.Min(score, maxScore);
                        }
                        // If no score property, give partial credit for having a response
                        return maxScore * 0.5m;

                    case JsonValueKind.Array:
                        // For array responses, give partial credit
                        return maxScore * 0.5m;

                    default:
                        return 0;
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static void EnsureAssignmentDateTimesAreUtc(Assignment assignment)
        {
            if (assignment == null) return;
            if (assignment.DueDate.HasValue && assignment.DueDate.Value.Kind != DateTimeKind.Utc)
                assignment.DueDate = DateTime.SpecifyKind(assignment.DueDate.Value, DateTimeKind.Utc);
            if (assignment.CreatedAt.Kind != DateTimeKind.Utc)
                assignment.CreatedAt = DateTime.SpecifyKind(assignment.CreatedAt, DateTimeKind.Utc);
            if (assignment.CompletedAt.HasValue && assignment.CompletedAt.Value.Kind != DateTimeKind.Utc)
                assignment.CompletedAt = DateTime.SpecifyKind(assignment.CompletedAt.Value, DateTimeKind.Utc);
        }

        private static void EnsureAuditDateTimesAreUtc(Audit audit)
        {
            if (audit == null) return;
            if (audit.StartTime.HasValue && audit.StartTime.Value.Kind != DateTimeKind.Utc)
                audit.StartTime = DateTime.SpecifyKind(audit.StartTime.Value, DateTimeKind.Utc);
            if (audit.EndTime.HasValue && audit.EndTime.Value.Kind != DateTimeKind.Utc)
                audit.EndTime = DateTime.SpecifyKind(audit.EndTime.Value, DateTimeKind.Utc);
            if (audit.CreatedAt.Kind != DateTimeKind.Utc)
                audit.CreatedAt = DateTime.SpecifyKind(audit.CreatedAt, DateTimeKind.Utc);
        }

        private static void EnsureTemplateDateTimesAreUtc(Template template)
        {
            if (template == null) return;
            if (template.ValidFrom.HasValue && template.ValidFrom.Value.Kind != DateTimeKind.Utc)
                template.ValidFrom = DateTime.SpecifyKind(template.ValidFrom.Value, DateTimeKind.Utc);
            if (template.ValidTo.HasValue && template.ValidTo.Value.Kind != DateTimeKind.Utc)
                template.ValidTo = DateTime.SpecifyKind(template.ValidTo.Value, DateTimeKind.Utc);
            if (template.CreatedAt.Kind != DateTimeKind.Utc)
                template.CreatedAt = DateTime.SpecifyKind(template.CreatedAt, DateTimeKind.Utc);
        }

        private static void EnsureUserDateTimesAreUtc(User user)
        {
            if (user == null) return;
            if (user.CreatedAt.Kind != DateTimeKind.Utc)
                user.CreatedAt = DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc);
        }

        public async Task<bool> DeleteAuditAsync(Guid auditId)
        {
            try
            {
                var audit = await _auditRepository.GetByIdAsync(auditId);
                if (audit == null)
                    return false;

                _auditRepository.Remove(audit);
                await _auditRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error deleting audit: {ex.Message}");
                return false;
            }
        }
    }
} 