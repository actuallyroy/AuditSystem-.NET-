using AuditSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Services
{
    public interface IAssignmentService
    {
        // Basic CRUD operations
        Task<Assignment> GetAssignmentByIdAsync(Guid assignmentId);
        Task<IEnumerable<Assignment>> GetAllAssignmentsAsync();
        Task<Assignment> CreateAssignmentAsync(Assignment assignment);
        Task<Assignment> UpdateAssignmentAsync(Assignment assignment);
        Task<bool> DeleteAssignmentAsync(Guid assignmentId);
        
        // Role-based access methods
        Task<IEnumerable<Assignment>> GetAssignmentsByRoleAsync(string role, Guid userId, Guid organisationId);
        Task<IEnumerable<Assignment>> GetAssignmentsByAuditorAsync(Guid auditorId);
        Task<IEnumerable<Assignment>> GetAssignmentsByManagerAsync(Guid managerId, Guid organisationId);
        
        // Organization-specific methods
        Task<IEnumerable<Assignment>> GetAssignmentsByOrganisationAsync(Guid organisationId);
        Task<IEnumerable<Assignment>> GetPendingAssignmentsAsync(Guid organisationId);
        Task<IEnumerable<Assignment>> GetOverdueAssignmentsAsync(Guid organisationId);
        
        // Template and status methods
        Task<IEnumerable<Assignment>> GetAssignmentsByTemplateAsync(Guid templateId);
        Task<IEnumerable<Assignment>> GetAssignmentsByStatusAsync(string status, Guid organisationId);
        
        // Assignment management methods
        Task<Assignment> AssignTemplateToAuditorAsync(Guid templateId, Guid auditorId, Guid assignerId, Assignment assignmentDetails);
        Task<bool> UnassignTemplateFromAuditorAsync(Guid templateId, Guid auditorId);
        Task<Assignment> UpdateAssignmentStatusAsync(Guid assignmentId, string status);
        
        // Validation methods
        Task<bool> CanUserAccessAssignmentAsync(Guid userId, Guid assignmentId, string userRole);
        Task<bool> CanUserManageAssignmentsAsync(Guid userId, Guid organisationId, string userRole);
    }
} 