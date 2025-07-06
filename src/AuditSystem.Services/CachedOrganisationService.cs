using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class CachedOrganisationService : IOrganisationService
    {
        private readonly IOrganisationService _organisationService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CachedOrganisationService> _logger;

        public CachedOrganisationService(
            IOrganisationService organisationService,
            ICacheService cacheService,
            ILogger<CachedOrganisationService> logger)
        {
            _organisationService = organisationService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Organisation> GetOrganisationByIdAsync(Guid organisationId)
        {
            var cacheKey = CacheKeys.OrganizationById(organisationId);
            var cachedOrganisation = await _cacheService.GetAsync<Organisation>(cacheKey);
            if (cachedOrganisation != null)
            {
                _logger.LogDebug("Organisation {OrganisationId} found in cache", organisationId);
                return cachedOrganisation;
            }
            var organisation = await _organisationService.GetOrganisationByIdAsync(organisationId);
            if (organisation != null)
            {
                await _cacheService.SetAsync(cacheKey, organisation, CacheKeys.OrganizationCacheExpiration);
                _logger.LogDebug("Organisation {OrganisationId} cached for {Expiration} minutes", organisationId, CacheKeys.OrganizationCacheExpiration.TotalMinutes);
            }
            return organisation;
        }

        public Task<Organisation> GetOrganisationByNameAsync(string name)
            => _organisationService.GetOrganisationByNameAsync(name);

        public Task<IEnumerable<Organisation>> GetAllOrganisationsAsync()
            => _organisationService.GetAllOrganisationsAsync();

        public Task<Organisation> CreateOrganisationAsync(Organisation organisation)
            => _organisationService.CreateOrganisationAsync(organisation);

        public Task<Organisation> UpdateOrganisationAsync(Organisation organisation)
            => _organisationService.UpdateOrganisationAsync(organisation);

        public Task<bool> DeleteOrganisationAsync(Guid organisationId)
            => _organisationService.DeleteOrganisationAsync(organisationId);

        public Task<Organisation> CreateDefaultOrganisationForUserAsync(string firstName)
            => _organisationService.CreateDefaultOrganisationForUserAsync(firstName);

        public Task<bool> InviteUserToOrganisationAsync(Guid organisationId, string email, string role)
            => _organisationService.InviteUserToOrganisationAsync(organisationId, email, role);

        public Task<bool> AcceptInvitationAsync(string token)
            => _organisationService.AcceptInvitationAsync(token);

        public Task<bool> JoinOrganisationAsync(Guid userId, Guid organisationId)
            => _organisationService.JoinOrganisationAsync(userId, organisationId);

        public Task<bool> RemoveUserFromOrganisationAsync(Guid userId, Guid organisationId)
            => _organisationService.RemoveUserFromOrganisationAsync(userId, organisationId);

        public Task<IEnumerable<Organisation>> GetOrganisationsForJoiningAsync()
            => _organisationService.GetOrganisationsForJoiningAsync();
    }
} 