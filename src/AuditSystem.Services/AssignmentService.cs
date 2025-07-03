using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Repositories;
using AuditSystem.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITemplateRepository _templateRepository;
        private readonly IOrganisationRepository _organisationRepository;

        public AssignmentService(
            IAssignmentRepository assignmentRepository,
            IUserRepository userRepository,
            ITemplateRepository templateRepository,
            IOrganisationRepository organisationRepository)
        {
            _assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
            _organisationRepository = organisationRepository ?? throw new ArgumentNullException(nameof(organisationRepository));
        }

        public async Task<Assignment> GetAssignmentByIdAsync(Guid assignmentId)
        {
            if (assignmentId == Guid.Empty)
                throw new ArgumentException("Assignment ID cannot be empty", nameof(assignmentId));

            return await _assignmentRepository.GetAssignmentWithDetailsAsync(assignmentId);
        }

        public async Task<IEnumerable<Assignment>> GetAllAssignmentsAsync()
        {
            return await _assignmentRepository.GetAllAsync();
        }

        public async Task<Assignment> CreateAssignmentAsync(Assignment assignment)
        {
            if (assignment == null)
                throw new ArgumentNullException(nameof(assignment));

            await ValidateAssignmentAsync(assignment);

            // Set initial values
            assignment.AssignmentId = Guid.NewGuid();
            assignment.CreatedAt = DateTime.UtcNow;
            assignment.Status = assignment.Status ?? "pending";

            await _assignmentRepository.AddAsync(assignment);
            await _assignmentRepository.SaveChangesAsync();

            return assignment;
        }

        public async Task<Assignment> UpdateAssignmentAsync(Assignment assignment)
        {
            if (assignment == null)
                throw new ArgumentNullException(nameof(assignment));

            var existingAssignment = await _assignmentRepository.GetByIdAsync(assignment.AssignmentId);
            if (existingAssignment == null)
                throw new InvalidOperationException($"Assignment with ID {assignment.AssignmentId} not found");

            await ValidateAssignmentAsync(assignment);

            // Preserve creation timestamp
            assignment.CreatedAt = existingAssignment.CreatedAt;

            _assignmentRepository.Update(assignment);
            await _assignmentRepository.SaveChangesAsync();

            return assignment;
        }

        public async Task<bool> DeleteAssignmentAsync(Guid assignmentId)
        {
            if (assignmentId == Guid.Empty)
                throw new ArgumentException("Assignment ID cannot be empty", nameof(assignmentId));

            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null)
                return false;

            _assignmentRepository.Remove(assignment);
            return await _assignmentRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByRoleAsync(string role, Guid userId, Guid organisationId)
        {
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Role cannot be empty", nameof(role));
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            if (organisationId == Guid.Empty)
                throw new ArgumentException("Organisation ID cannot be empty", nameof(organisationId));

            return role.ToLowerInvariant() switch
            {
                "administrator" => await _assignmentRepository.GetAllAsync(),
                "manager" or "supervisor" => await _assignmentRepository.GetAssignmentsByOrganisationAsync(organisationId),
                "auditor" => await _assignmentRepository.GetAssignmentsByAuditorAsync(userId),
                _ => new List<Assignment>()
            };
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByAuditorAsync(Guid auditorId)
        {
            if (auditorId == Guid.Empty)
                throw new ArgumentException("Auditor ID cannot be empty", nameof(auditorId));

            return await _assignmentRepository.GetAssignmentsByAuditorAsync(auditorId);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByManagerAsync(Guid managerId, Guid organisationId)
        {
            if (managerId == Guid.Empty)
                throw new ArgumentException("Manager ID cannot be empty", nameof(managerId));
            if (organisationId == Guid.Empty)
                throw new ArgumentException("Organisation ID cannot be empty", nameof(organisationId));

            return await _assignmentRepository.GetAssignmentsByAssignerAsync(managerId);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByOrganisationAsync(Guid organisationId)
        {
            if (organisationId == Guid.Empty)
                throw new ArgumentException("Organisation ID cannot be empty", nameof(organisationId));

            return await _assignmentRepository.GetAssignmentsByOrganisationAsync(organisationId);
        }

        public async Task<IEnumerable<Assignment>> GetPendingAssignmentsAsync(Guid organisationId)
        {
            if (organisationId == Guid.Empty)
                throw new ArgumentException("Organisation ID cannot be empty", nameof(organisationId));

            return await _assignmentRepository.GetPendingAssignmentsAsync(organisationId);
        }

        public async Task<IEnumerable<Assignment>> GetOverdueAssignmentsAsync(Guid organisationId)
        {
            if (organisationId == Guid.Empty)
                throw new ArgumentException("Organisation ID cannot be empty", nameof(organisationId));

            return await _assignmentRepository.GetOverdueAssignmentsAsync(organisationId);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByTemplateAsync(Guid templateId)
        {
            if (templateId == Guid.Empty)
                throw new ArgumentException("Template ID cannot be empty", nameof(templateId));

            return await _assignmentRepository.GetAssignmentsByTemplateAsync(templateId);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByStatusAsync(string status, Guid organisationId)
        {
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("Status cannot be empty", nameof(status));
            if (organisationId == Guid.Empty)
                throw new ArgumentException("Organisation ID cannot be empty", nameof(organisationId));

            var allByStatus = await _assignmentRepository.GetAssignmentsByStatusAsync(status);
            return allByStatus.Where(a => a.OrganisationId == organisationId);
        }

        public async Task<Assignment> AssignTemplateToAuditorAsync(Guid templateId, Guid auditorId, Guid assignerId, Assignment assignmentDetails)
        {
            if (templateId == Guid.Empty)
                throw new ArgumentException("Template ID cannot be empty", nameof(templateId));
            if (auditorId == Guid.Empty)
                throw new ArgumentException("Auditor ID cannot be empty", nameof(auditorId));
            if (assignerId == Guid.Empty)
                throw new ArgumentException("Assigner ID cannot be empty", nameof(assignerId));

            // Validate entities exist
            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template == null)
                throw new InvalidOperationException($"Template with ID {templateId} not found");

            var auditor = await _userRepository.GetByIdAsync(auditorId);
            if (auditor == null)
                throw new InvalidOperationException($"User with ID {auditorId} not found");

            var assigner = await _userRepository.GetByIdAsync(assignerId);
            if (assigner == null)
                throw new InvalidOperationException($"Assigner with ID {assignerId} not found");

            // Validate that auditor and assigner are in the same organization
            if (auditor.OrganisationId != assigner.OrganisationId)
                throw new InvalidOperationException("Cannot assign templates across different organisations");

            // Check if assignment already exists
            var existingAssignment = await _assignmentRepository.ExistsAsync(templateId, auditorId);
            if (existingAssignment)
                throw new InvalidOperationException("Assignment already exists for this template and auditor");

            var assignment = new Assignment
            {
                AssignmentId = Guid.NewGuid(),
                TemplateId = templateId,
                AssignedToId = auditorId,
                AssignedById = assignerId,
                OrganisationId = auditor.OrganisationId.Value,
                StoreInfo = assignmentDetails?.StoreInfo,
                DueDate = assignmentDetails?.DueDate,
                Priority = assignmentDetails?.Priority ?? "medium",
                Notes = assignmentDetails?.Notes,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            await _assignmentRepository.AddAsync(assignment);
            await _assignmentRepository.SaveChangesAsync();

            return assignment;
        }

        public async Task<bool> UnassignTemplateFromAuditorAsync(Guid templateId, Guid auditorId)
        {
            if (templateId == Guid.Empty)
                throw new ArgumentException("Template ID cannot be empty", nameof(templateId));
            if (auditorId == Guid.Empty)
                throw new ArgumentException("Auditor ID cannot be empty", nameof(auditorId));

            var assignments = await _assignmentRepository.FindAsync(a => 
                a.TemplateId == templateId && a.AssignedToId == auditorId);
            
            var assignment = assignments.FirstOrDefault();
            if (assignment == null)
                return false;

            _assignmentRepository.Remove(assignment);
            return await _assignmentRepository.SaveChangesAsync();
        }

        public async Task<Assignment> UpdateAssignmentStatusAsync(Guid assignmentId, string status)
        {
            if (assignmentId == Guid.Empty)
                throw new ArgumentException("Assignment ID cannot be empty", nameof(assignmentId));
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("Status cannot be empty", nameof(status));

            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null)
                throw new InvalidOperationException($"Assignment with ID {assignmentId} not found");

            ValidateStatus(status);

            assignment.Status = status;
            _assignmentRepository.Update(assignment);
            await _assignmentRepository.SaveChangesAsync();

            return assignment;
        }

        public async Task<bool> CanUserAccessAssignmentAsync(Guid userId, Guid assignmentId, string userRole)
        {
            if (userId == Guid.Empty || assignmentId == Guid.Empty || string.IsNullOrWhiteSpace(userRole))
                return false;

            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null)
                return false;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            return userRole.ToLowerInvariant() switch
            {
                "administrator" => true,
                "manager" or "supervisor" => assignment.OrganisationId == user.OrganisationId,
                "auditor" => assignment.AssignedToId == userId,
                _ => false
            };
        }

        public async Task<bool> CanUserManageAssignmentsAsync(Guid userId, Guid organisationId, string userRole)
        {
            if (userId == Guid.Empty || organisationId == Guid.Empty || string.IsNullOrWhiteSpace(userRole))
                return false;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            return userRole.ToLowerInvariant() switch
            {
                "administrator" => true,
                "manager" or "supervisor" => user.OrganisationId == organisationId,
                _ => false
            };
        }

        private async Task ValidateAssignmentAsync(Assignment assignment)
        {
            if (assignment.TemplateId == Guid.Empty)
                throw new ArgumentException("Template ID is required");
            if (assignment.AssignedToId == Guid.Empty)
                throw new ArgumentException("Assigned To ID is required");
            if (assignment.AssignedById == Guid.Empty)
                throw new ArgumentException("Assigned By ID is required");
            if (assignment.OrganisationId == Guid.Empty)
                throw new ArgumentException("Organisation ID is required");

            // Validate referenced entities exist
            var template = await _templateRepository.GetByIdAsync(assignment.TemplateId);
            if (template == null)
                throw new InvalidOperationException("Referenced template does not exist");

            var assignedTo = await _userRepository.GetByIdAsync(assignment.AssignedToId);
            if (assignedTo == null)
                throw new InvalidOperationException("Assigned user does not exist");

            var assignedBy = await _userRepository.GetByIdAsync(assignment.AssignedById);
            if (assignedBy == null)
                throw new InvalidOperationException("Assigning user does not exist");

            // Validate organization consistency
            if (assignedTo.OrganisationId != assignment.OrganisationId)
                throw new InvalidOperationException("Assigned user must belong to the same organisation");

            if (assignedBy.OrganisationId != assignment.OrganisationId)
                throw new InvalidOperationException("Assigning user must belong to the same organisation");

            // Validate status
            if (!string.IsNullOrWhiteSpace(assignment.Status))
                ValidateStatus(assignment.Status);

            // Validate priority
            if (!string.IsNullOrWhiteSpace(assignment.Priority))
                ValidatePriority(assignment.Priority);
        }

        private static void ValidateStatus(string status)
        {
            var validStatuses = new[] { "pending", "cancelled", "expired", "fulfilled" };
            if (!validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"Invalid status. Valid statuses are: {string.Join(", ", validStatuses)}");
        }

        private static void ValidatePriority(string priority)
        {
            var validPriorities = new[] { "Low", "Medium", "High", "Critical" };
            if (!validPriorities.Contains(priority, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"Invalid priority. Valid priorities are: {string.Join(", ", validPriorities)}");
        }
    }
} 