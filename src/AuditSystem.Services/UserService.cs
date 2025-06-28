using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Repositories;
using AuditSystem.Domain.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
                return false;

            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return false;

            return true;
        }

        public async Task<User> CreateUserAsync(User user, string password)
        {
            // Validation
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required", nameof(password));

            if (await _userRepository.UsernameExistsAsync(user.Username))
                throw new InvalidOperationException($"Username '{user.Username}' is already taken");

            if (user.Email != null && await _userRepository.EmailExistsAsync(user.Email))
                throw new InvalidOperationException($"Email '{user.Email}' is already registered");

            // Create password hash
            CreatePasswordHash(password, out var passwordHash, out var passwordSalt);

            // Set user properties
            user.UserId = Guid.NewGuid();
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            // Add user to repository
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return user;
        }

        public async Task<bool> DeactivateUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            user.IsActive = false;
            _userRepository.Update(user);
            
            return await _userRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllAsync();
        }

        public async Task<User> GetUserByIdAsync(Guid userId)
        {
            return await _userRepository.GetByIdAsync(userId);
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _userRepository.GetByUsernameAsync(username);
        }

        public async Task<IEnumerable<User>> GetUsersByOrganisationAsync(Guid organisationId)
        {
            // Handle nullable OrganisationId in the query
            return await _userRepository.FindAsync(u => u.OrganisationId.HasValue && u.OrganisationId.Value == organisationId);
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role)
        {
            return await _userRepository.FindAsync(u => u.Role == role);
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            var existingUser = await _userRepository.GetByIdAsync(user.UserId);
            if (existingUser == null)
                throw new KeyNotFoundException($"User with ID {user.UserId} not found");

            // Update properties (except password-related ones)
            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            existingUser.Email = user.Email;
            existingUser.Phone = user.Phone;
            existingUser.Role = user.Role;
            existingUser.IsActive = user.IsActive;

            _userRepository.Update(existingUser);
            await _userRepository.SaveChangesAsync();

            return existingUser;
        }

        public async Task<bool> UpdateUserPasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                return false;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            // Verify current password
            if (!VerifyPasswordHash(currentPassword, user.PasswordHash, user.PasswordSalt))
                return false;

            // Create new password hash
            CreatePasswordHash(newPassword, out var passwordHash, out var passwordSalt);
            
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            
            _userRepository.Update(user);
            
            return await _userRepository.SaveChangesAsync();
        }

        #region Password Helpers
        private static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            using var hmac = new HMACSHA512();
            var saltBytes = hmac.Key;
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            passwordSalt = Convert.ToBase64String(saltBytes);
            passwordHash = Convert.ToBase64String(hashBytes);
        }

        private static bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrWhiteSpace(password) || 
                string.IsNullOrWhiteSpace(storedHash) || 
                string.IsNullOrWhiteSpace(storedSalt))
                return false;

            try
            {
                var saltBytes = Convert.FromBase64String(storedSalt);
                var hashBytes = Convert.FromBase64String(storedHash);

                using var hmac = new HMACSHA512(saltBytes);
                var computedHashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Compare computed hash with stored hash
                for (int i = 0; i < computedHashBytes.Length; i++)
                {
                    if (hashBytes[i] != computedHashBytes[i])
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
} 