using AuditSystem.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Repositories
{
    public interface IOrganisationRepository : IRepository<Organisation>
    {
        Task<Organisation> GetByNameAsync(string name);
    }
} 