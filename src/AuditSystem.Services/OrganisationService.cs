using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Repositories;
using AuditSystem.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class OrganisationService : IOrganisationService
    {
        private readonly IOrganisationRepository _organisationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRepository<OrganisationInvitation> _invitationRepository;

        public OrganisationService(
            IOrganisationRepository organisationRepository,
            IUserRepository userRepository,
            IRepository<OrganisationInvitation> invitationRepository)
        {
            _organisationRepository = organisationRepository;
            _userRepository = userRepository;
            _invitationRepository = invitationRepository;
        }

        public async Task<Organisation> CreateDefaultOrganisationForUserAsync(string firstName)
        {
            var organisationName = $"{firstName}'s Organisation";
            
            // Check if organisation with this name already exists
            var existingOrg = await _organisationRepository.GetByNameAsync(organisationName);
            if (existingOrg != null)
            {
                // If it exists, add a timestamp to make it unique
                organisationName = $"{organisationName} {DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
            }

            var organisation = new Organisation
            {
                OrganisationId = Guid.NewGuid(),
                Name = organisationName,
                Region = "Default Region",
                Type = "Personal",
                CreatedAt = DateTime.UtcNow
            };

            await _organisationRepository.AddAsync(organisation);
            await _organisationRepository.SaveChangesAsync();

            return organisation;
        }

        public async Task<Organisation> CreateOrganisationAsync(Organisation organisation)
        {
            if (organisation == null)
                throw new ArgumentNullException(nameof(organisation));

            organisation.OrganisationId = Guid.NewGuid();
            organisation.CreatedAt = DateTime.UtcNow;

            await _organisationRepository.AddAsync(organisation);
            await _organisationRepository.SaveChangesAsync();

            return organisation;
        }

        public async Task<bool> DeleteOrganisationAsync(Guid organisationId)
        {
            var organisation = await _organisationRepository.GetByIdAsync(organisationId);
            if (organisation == null)
                return false;

            _organisationRepository.Remove(organisation);
            return await _organisationRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<Organisation>> GetAllOrganisationsAsync()
        {
            return await _organisationRepository.GetAllAsync();
        }

        public async Task<Organisation> GetOrganisationByIdAsync(Guid organisationId)
        {
            return await _organisationRepository.GetByIdAsync(organisationId);
        }

        public async Task<Organisation> GetOrganisationByNameAsync(string name)
        {
            return await _organisationRepository.GetByNameAsync(name);
        }

        public async Task<Organisation> UpdateOrganisationAsync(Organisation organisation)
        {
            if (organisation == null)
                throw new ArgumentNullException(nameof(organisation));

            var existingOrganisation = await _organisationRepository.GetByIdAsync(organisation.OrganisationId);
            if (existingOrganisation == null)
                throw new KeyNotFoundException($"Organisation with ID {organisation.OrganisationId} not found");

            existingOrganisation.Name = organisation.Name;
            existingOrganisation.Region = organisation.Region;
            existingOrganisation.Type = organisation.Type;

            _organisationRepository.Update(existingOrganisation);
            await _organisationRepository.SaveChangesAsync();

            return existingOrganisation;
        }

        // New methods for organization invitations
        public async Task<bool> InviteUserToOrganisationAsync(Guid organisationId, string email, string role)
        {
            var organisation = await _organisationRepository.GetByIdAsync(organisationId);
            if (organisation == null)
                throw new KeyNotFoundException($"Organisation with ID {organisationId} not found");

            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(email);
            
            // Generate token for invitation
            string token = Guid.NewGuid().ToString();
            
            // Create invitation record
            var invitation = new OrganisationInvitation
            {
                InvitationId = Guid.NewGuid(),
                OrganisationId = organisationId,
                Email = email,
                Token = token,
                Role = role,
                UserId = existingUser?.UserId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Status = "Pending"
            };

            await _invitationRepository.AddAsync(invitation);
            await _invitationRepository.SaveChangesAsync();

            // In a real-world implementation, send email with invitation link
            // _emailService.SendInvitationEmail(email, organisation.Name, token);

            return true;
        }

        public async Task<bool> AcceptInvitationAsync(string token)
        {
            var invitation = (await _invitationRepository.FindAsync(i => i.Token == token && i.Status == "Pending"))
                .FirstOrDefault();

            if (invitation == null)
                return false;

            // Check if invitation is expired
            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                invitation.Status = "Expired";
                _invitationRepository.Update(invitation);
                await _invitationRepository.SaveChangesAsync();
                return false;
            }

            // Mark invitation as accepted
            invitation.Status = "Accepted";
            _invitationRepository.Update(invitation);

            // If user doesn't exist, they will need to register first
            if (invitation.UserId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(invitation.UserId.Value);
                if (user != null)
                {
                    user.OrganisationId = invitation.OrganisationId;
                    if (!string.IsNullOrEmpty(invitation.Role))
                    {
                        user.Role = invitation.Role;
                    }
                    _userRepository.Update(user);
                }
            }

            await _invitationRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> JoinOrganisationAsync(Guid userId, Guid organisationId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            var organisation = await _organisationRepository.GetByIdAsync(organisationId);
            if (organisation == null)
                return false;

            // Update user's organization ID
            user.OrganisationId = organisationId;
            _userRepository.Update(user);

            return await _userRepository.SaveChangesAsync();
        }

        public async Task<bool> RemoveUserFromOrganisationAsync(Guid userId, Guid organisationId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.OrganisationId.HasValue || user.OrganisationId.Value != organisationId)
                return false;

            // Set organization ID to null
            user.OrganisationId = null;
            _userRepository.Update(user);

            return await _userRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<Organisation>> GetOrganisationsForJoiningAsync()
        {
            // Return only organizations that are set to allow public joining
            // This is a simplified implementation - in a real app, you might have an "IsPublic" flag
            return await _organisationRepository.FindAsync(o => o.Type != "Personal");
        }
    }
} 