using System;
using System.Collections.Generic;

namespace AuditSystem.Domain.Entities
{
    public class Organisation
    {
        public Guid OrganisationId { get; set; }
        public string Name { get; set; }
        public string Region { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<User> Users { get; set; }
        public virtual ICollection<Assignment> Assignments { get; set; }
        public virtual ICollection<Audit> Audits { get; set; }
    }
} 