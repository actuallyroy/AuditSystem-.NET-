using AuditSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Repositories
{
    public interface IAuditRepository : IRepository<Audit>
    {
        Task<IEnumerable<Audit>> GetAuditsByAuditorIdAsync(Guid auditorId);
        Task<IEnumerable<Audit>> GetAuditsByOrganisationIdAsync(Guid organisationId);
        Task<IEnumerable<Audit>> GetAuditsByTemplateIdAsync(Guid templateId);
        Task<IEnumerable<Audit>> GetAllAuditsWithNavigationPropertiesAsync();
        Task<IEnumerable<Audit>> GetAuditsPendingSyncAsync();
        Task<IEnumerable<Audit>> GetFlaggedAuditsAsync();
        Task<IEnumerable<Audit>> GetAuditsByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
} 