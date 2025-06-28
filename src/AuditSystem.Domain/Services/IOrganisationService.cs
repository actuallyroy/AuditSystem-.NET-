using AuditSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Services
{
    public interface IOrganisationService
    {
        Task<Organisation> GetOrganisationByIdAsync(Guid organisationId);
        Task<Organisation> GetOrganisationByNameAsync(string name);
        Task<IEnumerable<Organisation>> GetAllOrganisationsAsync();
        Task<Organisation> CreateOrganisationAsync(Organisation organisation);
        Task<Organisation> UpdateOrganisationAsync(Organisation organisation);
        Task<bool> DeleteOrganisationAsync(Guid organisationId);
        Task<Organisation> CreateDefaultOrganisationForUserAsync(string firstName);
        
        // New methods for organization invitations
        Task<bool> InviteUserToOrganisationAsync(Guid organisationId, string email, string role);
        Task<bool> AcceptInvitationAsync(string token);
        Task<bool> JoinOrganisationAsync(Guid userId, Guid organisationId);
        Task<bool> RemoveUserFromOrganisationAsync(Guid userId, Guid organisationId);
        Task<IEnumerable<Organisation>> GetOrganisationsForJoiningAsync();
    }
} 