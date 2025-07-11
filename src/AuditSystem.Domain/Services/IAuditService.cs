using AuditSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Services
{
    public interface IAuditService
    {
        Task<Audit> GetAuditByIdAsync(Guid auditId);
        Task<Audit?> GetAuditByAssignmentAsync(Guid assignmentId);
        Task<IEnumerable<Audit>> GetAllAuditsAsync();
        Task<IEnumerable<Audit>> GetAuditsByAuditorAsync(Guid auditorId);
        Task<IEnumerable<Audit>> GetAuditsByOrganisationAsync(Guid organisationId);
        Task<IEnumerable<Audit>> GetAuditsByTemplateAsync(Guid templateId);
        Task<IEnumerable<Audit>> GetAuditsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Audit> StartAuditAsync(Guid templateId, Guid auditorId, Guid organisationId, Guid assignmentId);
        Task<Audit> StartAuditFromAssignmentAsync(Guid templateId, Guid auditorId, Guid organisationId, Guid assignmentId, JsonDocument? storeInfo = null, JsonDocument? location = null);
        Task<Audit> SubmitAuditAsync(Audit audit);
        Task<bool> SyncAuditAsync(Guid auditId);
        Task<Audit> UpdateAuditStatusAsync(Guid auditId, string status);
        Task<Audit> FlagAuditAsync(Guid auditId, bool isFlagged);
        Task<Audit> AddManagerNotesAsync(Guid auditId, string notes);
        Task<Audit> RecalculateAuditScoreAsync(Guid auditId);
        Task<Audit> UpdateAuditAsync(Audit audit);
        Task<decimal> CalculateAuditScoreAsync(Audit audit);
        Task<bool> DeleteAuditAsync(Guid auditId);
    }
} 