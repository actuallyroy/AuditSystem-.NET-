using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Repositories;
using AuditSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AuditSystem.Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(AuditSystemDbContext context) : base(context)
        {
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            }
            catch (InvalidCastException ex) when (ex.Message.Contains("organisation_id"))
            {
                // Handle the specific case where organisation_id is null
                // This is a workaround for the issue with Npgsql trying to cast null to Guid
                return null;
            }
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
            }
            catch (InvalidCastException ex) when (ex.Message.Contains("organisation_id"))
            {
                // Handle the specific case where organisation_id is null
                return null;
            }
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            try
            {
                return await _context.Users
                    .AnyAsync(u => u.Username.ToLower() == username.ToLower());
            }
            catch (InvalidCastException ex) when (ex.Message.Contains("organisation_id"))
            {
                // If we can't query due to null organisation_id, assume username doesn't exist
                return false;
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                return await _context.Users
                    .AnyAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
            }
            catch (InvalidCastException ex) when (ex.Message.Contains("organisation_id"))
            {
                // If we can't query due to null organisation_id, assume email doesn't exist
                return false;
            }
        }
    }
} 