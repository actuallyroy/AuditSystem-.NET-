using AuditSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Services
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(Guid userId);
        Task<User> GetUserByUsernameAsync(string username);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<IEnumerable<User>> GetUsersByOrganisationAsync(Guid organisationId);
        Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
        Task<User> CreateUserAsync(User user, string password);
        Task<User> UpdateUserAsync(User user);
        Task<bool> UpdateUserPasswordAsync(Guid userId, string currentPassword, string newPassword);
        Task<bool> DeactivateUserAsync(Guid userId);
        Task<bool> AuthenticateAsync(string username, string password);
    }
} 