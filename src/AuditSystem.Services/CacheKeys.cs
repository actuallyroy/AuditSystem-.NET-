using System;

namespace AuditSystem.Services
{
    public static class CacheKeys
    {
        // Cache prefixes
        private const string USER_PREFIX = "user";
        private const string TEMPLATE_PREFIX = "template";
        private const string ORGANIZATION_PREFIX = "org";
        private const string AUDIT_PREFIX = "audit";
        private const string DASHBOARD_PREFIX = "dashboard";
        private const string SESSION_PREFIX = "session";
        
        // Cache expiration times
        public static readonly TimeSpan UserCacheExpiration = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan TemplateCacheExpiration = TimeSpan.FromMinutes(60);
        public static readonly TimeSpan OrganizationCacheExpiration = TimeSpan.FromMinutes(60);
        public static readonly TimeSpan AuditCacheExpiration = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan DashboardCacheExpiration = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan SessionCacheExpiration = TimeSpan.FromHours(8);
        
        // User cache keys
        public static string UserById(Guid userId) => $"{USER_PREFIX}:id:{userId}";
        public static string UserByUsername(string username) => $"{USER_PREFIX}:username:{username.ToLower()}";
        public static string UsersByOrganization(Guid organizationId) => $"{USER_PREFIX}:org:{organizationId}";
        public static string UsersByRole(string role) => $"{USER_PREFIX}:role:{role.ToLower()}";
        public static string UserPattern(Guid userId) => $"{USER_PREFIX}:*:{userId}*";
        
        // Template cache keys
        public static string TemplateById(Guid templateId) => $"{TEMPLATE_PREFIX}:id:{templateId}";
        public static string TemplatesByUser(Guid userId) => $"{TEMPLATE_PREFIX}:user:{userId}";
        public static string PublishedTemplates() => $"{TEMPLATE_PREFIX}:published";
        public static string PublishedTemplatesByUser(Guid userId) => $"{TEMPLATE_PREFIX}:published:user:{userId}";
        public static string TemplatesByCategory(string category) => $"{TEMPLATE_PREFIX}:category:{category.ToLower()}";
        public static string TemplatesByCategoryAndUser(string category, Guid userId) => $"{TEMPLATE_PREFIX}:category:{category.ToLower()}:user:{userId}";
        public static string AssignedTemplates(Guid auditorId) => $"{TEMPLATE_PREFIX}:assigned:{auditorId}";
        public static string TemplatePattern(Guid templateId) => $"{TEMPLATE_PREFIX}:*:{templateId}*";
        public static string UserTemplatesPattern(Guid userId) => $"{TEMPLATE_PREFIX}:*:{userId}*";
        
        // Organization cache keys
        public static string OrganizationById(Guid organizationId) => $"{ORGANIZATION_PREFIX}:id:{organizationId}";
        public static string OrganizationByName(string name) => $"{ORGANIZATION_PREFIX}:name:{name.ToLower()}";
        public static string OrganizationInvitations(Guid organizationId) => $"{ORGANIZATION_PREFIX}:invitations:{organizationId}";
        public static string OrganizationPattern(Guid organizationId) => $"{ORGANIZATION_PREFIX}:*:{organizationId}*";
        
        // Audit cache keys
        public static string AuditById(Guid auditId) => $"{AUDIT_PREFIX}:id:{auditId}";
        public static string AuditsByUser(Guid userId) => $"{AUDIT_PREFIX}:user:{userId}";
        public static string AuditsByTemplate(Guid templateId) => $"{AUDIT_PREFIX}:template:{templateId}";
        public static string AuditsByOrganization(Guid organizationId) => $"{AUDIT_PREFIX}:org:{organizationId}";
        public static string AuditsByStatus(string status) => $"{AUDIT_PREFIX}:status:{status.ToLower()}";
        public static string AuditPattern(Guid auditId) => $"{AUDIT_PREFIX}:*:{auditId}*";
        
        // Dashboard cache keys
        public static string DashboardMetrics(Guid? organizationId = null) => 
            organizationId.HasValue ? $"{DASHBOARD_PREFIX}:metrics:org:{organizationId}" : $"{DASHBOARD_PREFIX}:metrics:global";
        public static string DashboardUserPerformance(Guid userId) => $"{DASHBOARD_PREFIX}:performance:user:{userId}";
        public static string DashboardTemplateStats(Guid templateId) => $"{DASHBOARD_PREFIX}:template-stats:{templateId}";
        public static string DashboardAuditTrends(Guid? organizationId = null) => 
            organizationId.HasValue ? $"{DASHBOARD_PREFIX}:trends:org:{organizationId}" : $"{DASHBOARD_PREFIX}:trends:global";
        public static string DashboardPattern() => $"{DASHBOARD_PREFIX}:*";
        
        // Session cache keys
        public static string UserSession(Guid userId) => $"{SESSION_PREFIX}:user:{userId}";
        public static string ActiveSessions(Guid userId) => $"{SESSION_PREFIX}:active:{userId}";
        public static string SessionPattern(Guid userId) => $"{SESSION_PREFIX}:*:{userId}*";
        
        // Rate limiting keys
        public static string RateLimit(string identifier) => $"rate_limit:{identifier}";
        
        // API response cache keys
        public static string ApiResponse(string controller, string action, string parameters) => 
            $"api:{controller.ToLower()}:{action.ToLower()}:{parameters}";
        
        // Health check keys
        public static string HealthCheck() => "health:check";
        
        // Helper methods for pattern matching
        public static string AllUserKeys() => $"{USER_PREFIX}:*";
        public static string AllTemplateKeys() => $"{TEMPLATE_PREFIX}:*";
        public static string AllOrganizationKeys() => $"{ORGANIZATION_PREFIX}:*";
        public static string AllAuditKeys() => $"{AUDIT_PREFIX}:*";
        public static string AllDashboardKeys() => $"{DASHBOARD_PREFIX}:*";
        public static string AllSessionKeys() => $"{SESSION_PREFIX}:*";
    }
} 