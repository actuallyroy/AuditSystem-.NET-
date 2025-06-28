using System;
using System.Collections.Generic;

namespace AuditSystem.Domain.Entities
{
    public class User
    {
        public Guid UserId { get; set; }
        public Guid? OrganisationId { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual Organisation Organisation { get; set; }
        public virtual ICollection<Template> CreatedTemplates { get; set; }
        public virtual ICollection<Assignment> AssignedByAssignments { get; set; }
        public virtual ICollection<Assignment> AssignedToAssignments { get; set; }
        public virtual ICollection<Audit> Audits { get; set; }
        public virtual ICollection<Report> Reports { get; set; }
        public virtual ICollection<Log> Logs { get; set; }
    }
} 