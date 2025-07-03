using AuditSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Repositories
{
    public interface IAssignmentRepository : IRepository<Assignment>
    {
        Task<IEnumerable<Assignment>> GetAssignmentsByOrganisationAsync(Guid organisationId);
        Task<IEnumerable<Assignment>> GetAssignmentsByAuditorAsync(Guid auditorId);
        Task<IEnumerable<Assignment>> GetAssignmentsByAssignerAsync(Guid assignerId);
        Task<IEnumerable<Assignment>> GetAssignmentsByTemplateAsync(Guid templateId);
        Task<IEnumerable<Assignment>> GetAssignmentsByStatusAsync(string status);
        Task<IEnumerable<Assignment>> GetPendingAssignmentsAsync(Guid organisationId);
        Task<IEnumerable<Assignment>> GetOverdueAssignmentsAsync(Guid organisationId);
        Task<Assignment> GetAssignmentWithDetailsAsync(Guid assignmentId);
        Task<bool> ExistsAsync(Guid templateId, Guid auditorId);
    }
} 