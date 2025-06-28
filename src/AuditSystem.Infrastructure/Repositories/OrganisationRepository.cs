using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Repositories;
using AuditSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AuditSystem.Infrastructure.Repositories
{
    public class OrganisationRepository : Repository<Organisation>, IOrganisationRepository
    {
        public OrganisationRepository(AuditSystemDbContext context) : base(context)
        {
        }

        public async Task<Organisation> GetByNameAsync(string name)
        {
            return await _context.Organisations
                .FirstOrDefaultAsync(o => o.Name.ToLower() == name.ToLower());
        }
    }
} 