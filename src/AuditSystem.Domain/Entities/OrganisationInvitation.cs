using System;

namespace AuditSystem.Domain.Entities
{
    public class OrganisationInvitation
    {
        public Guid InvitationId { get; set; }
        public Guid OrganisationId { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public string Role { get; set; }
        public Guid? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Status { get; set; } // "Pending", "Accepted", "Rejected", "Expired"
        
        // Navigation properties
        public virtual Organisation Organisation { get; set; }
        public virtual User User { get; set; }
    }
} 