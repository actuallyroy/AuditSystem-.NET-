using AuditSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AuditSystem.Infrastructure.Data
{
    public class AuditSystemDbContext : DbContext
    {
        public AuditSystemDbContext(DbContextOptions<AuditSystemDbContext> options)
            : base(options)
        {
        }

        public DbSet<Organisation> Organisations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Audit> Audits { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<OrganisationInvitation> OrganisationInvitations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity mappings
            ConfigureOrganisation(modelBuilder);
            ConfigureUser(modelBuilder);
            ConfigureTemplate(modelBuilder);
            ConfigureAssignment(modelBuilder);
            ConfigureAudit(modelBuilder);
            ConfigureReport(modelBuilder);
            ConfigureLog(modelBuilder);
            ConfigureOrganisationInvitation(modelBuilder);
        }

        private void ConfigureOrganisation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Organisation>(entity =>
            {
                entity.ToTable("organisation");
                entity.HasKey(e => e.OrganisationId);

                entity.Property(e => e.OrganisationId)
                    .HasColumnName("organisation_id")
                    .ValueGeneratedNever();

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .IsRequired();

                entity.Property(e => e.Region)
                    .HasColumnName("region");

                entity.Property(e => e.Type)
                    .HasColumnName("type");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("NOW()");
            });
        }

        private void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.UserId);

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .ValueGeneratedNever();

                entity.Property(e => e.OrganisationId)
                    .HasColumnName("organisation_id");

                entity.Property(e => e.Username)
                    .HasColumnName("username")
                    .IsRequired();

                entity.Property(e => e.FirstName)
                    .HasColumnName("first_name")
                    .IsRequired();

                entity.Property(e => e.LastName)
                    .HasColumnName("last_name")
                    .IsRequired();

                entity.Property(e => e.Email)
                    .HasColumnName("email");

                entity.Property(e => e.Phone)
                    .HasColumnName("phone");

                entity.Property(e => e.Role)
                    .HasColumnName("role")
                    .IsRequired();

                entity.Property(e => e.PasswordHash)
                    .HasColumnName("password_hash");

                entity.Property(e => e.PasswordSalt)
                    .HasColumnName("password_salt");

                entity.Property(e => e.IsActive)
                    .HasColumnName("is_active")
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("NOW()");

                entity.HasOne(d => d.Organisation)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.OrganisationId);

                entity.HasIndex(e => e.Username)
                    .IsUnique();

                entity.HasIndex(e => e.Email)
                    .IsUnique();
            });
        }

        private void ConfigureTemplate(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Template>(entity =>
            {
                entity.ToTable("template");
                entity.HasKey(e => e.TemplateId);

                entity.Property(e => e.TemplateId)
                    .HasColumnName("template_id")
                    .ValueGeneratedNever();

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .IsRequired();

                entity.Property(e => e.Description)
                    .HasColumnName("description");

                entity.Property(e => e.Category)
                    .HasColumnName("category");

                entity.Property(e => e.Questions)
                    .HasColumnName("questions")
                    .HasColumnType("jsonb")
                    .IsRequired();

                entity.Property(e => e.ScoringRules)
                    .HasColumnName("scoring_rules")
                    .HasColumnType("jsonb");

                entity.Property(e => e.ValidFrom)
                    .HasColumnName("valid_from");

                entity.Property(e => e.ValidTo)
                    .HasColumnName("valid_to");

                entity.Property(e => e.CreatedById)
                    .HasColumnName("created_by");

                entity.Property(e => e.IsPublished)
                    .HasColumnName("is_published")
                    .HasDefaultValue(false);

                entity.Property(e => e.Version)
                    .HasColumnName("version")
                    .HasDefaultValue(1);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("NOW()");

                entity.HasOne(d => d.CreatedBy)
                    .WithMany(p => p.CreatedTemplates)
                    .HasForeignKey(d => d.CreatedById);
            });
        }

        private void ConfigureAssignment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Assignment>(entity =>
            {
                entity.ToTable("assignment");
                entity.HasKey(e => e.AssignmentId);

                entity.Property(e => e.AssignmentId)
                    .HasColumnName("assignment_id")
                    .ValueGeneratedNever();

                entity.Property(e => e.TemplateId)
                    .HasColumnName("template_id");

                entity.Property(e => e.AssignedToId)
                    .HasColumnName("assigned_to");

                entity.Property(e => e.AssignedById)
                    .HasColumnName("assigned_by");

                entity.Property(e => e.OrganisationId)
                    .HasColumnName("organisation_id");

                entity.Property(e => e.StoreInfo)
                    .HasColumnName("store_info")
                    .HasColumnType("jsonb");

                entity.Property(e => e.DueDate)
                    .HasColumnName("due_date");

                entity.Property(e => e.Priority)
                    .HasColumnName("priority");

                entity.Property(e => e.Notes)
                    .HasColumnName("notes");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValue("pending");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("NOW()");

                // Ignore CompletedAt property until database column is added
                entity.Ignore(e => e.CompletedAt);

                entity.HasOne(d => d.Template)
                    .WithMany(p => p.Assignments)
                    .HasForeignKey(d => d.TemplateId);

                entity.HasOne(d => d.AssignedTo)
                    .WithMany(p => p.AssignedToAssignments)
                    .HasForeignKey(d => d.AssignedToId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.AssignedBy)
                    .WithMany(p => p.AssignedByAssignments)
                    .HasForeignKey(d => d.AssignedById)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Organisation)
                    .WithMany(p => p.Assignments)
                    .HasForeignKey(d => d.OrganisationId);
            });
        }

        private void ConfigureAudit(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Audit>(entity =>
            {
                entity.ToTable("audit");
                entity.HasKey(e => e.AuditId);

                entity.Property(e => e.AuditId)
                    .HasColumnName("audit_id")
                    .ValueGeneratedNever();

                entity.Property(e => e.TemplateId)
                    .HasColumnName("template_id");

                entity.Property(e => e.TemplateVersion)
                    .HasColumnName("template_version")
                    .IsRequired(false);

                entity.Property(e => e.AuditorId)
                    .HasColumnName("auditor_id");

                entity.Property(e => e.OrganisationId)
                    .HasColumnName("organisation_id");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .IsRequired();

                entity.Property(e => e.StartTime)
                    .HasColumnName("start_time");

                entity.Property(e => e.EndTime)
                    .HasColumnName("end_time");

                entity.Property(e => e.StoreInfo)
                    .HasColumnName("store_info")
                    .HasColumnType("jsonb");

                entity.Property(e => e.Responses)
                    .HasColumnName("responses")
                    .HasColumnType("jsonb")
                    .IsRequired(false);

                entity.Property(e => e.Media)
                    .HasColumnName("media")
                    .HasColumnType("jsonb");

                entity.Property(e => e.Location)
                    .HasColumnName("location")
                    .HasColumnType("jsonb");

                entity.Property(e => e.Score)
                    .HasColumnName("score");

                entity.Property(e => e.CriticalIssues)
                    .HasColumnName("critical_issues")
                    .HasDefaultValue(0);

                entity.Property(e => e.ManagerNotes)
                    .HasColumnName("manager_notes");

                entity.Property(e => e.IsFlagged)
                    .HasColumnName("is_flagged")
                    .HasDefaultValue(false);

                entity.Property(e => e.SyncFlag)
                    .HasColumnName("sync_flag")
                    .HasDefaultValue(false);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("NOW()");

                entity.HasOne(d => d.Template)
                    .WithMany(p => p.Audits)
                    .HasForeignKey(d => d.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Auditor)
                    .WithMany(p => p.Audits)
                    .HasForeignKey(d => d.AuditorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Organisation)
                    .WithMany(p => p.Audits)
                    .HasForeignKey(d => d.OrganisationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureReport(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Report>(entity =>
            {
                entity.ToTable("report");
                entity.HasKey(e => e.ReportId);

                entity.Property(e => e.ReportId)
                    .HasColumnName("report_id")
                    .ValueGeneratedNever();

                entity.Property(e => e.GeneratedById)
                    .HasColumnName("generated_by");

                entity.Property(e => e.Name)
                    .HasColumnName("name");

                entity.Property(e => e.ReportType)
                    .HasColumnName("report_type");

                entity.Property(e => e.Format)
                    .HasColumnName("format");

                entity.Property(e => e.FiltersApplied)
                    .HasColumnName("filters_applied")
                    .HasColumnType("jsonb");

                entity.Property(e => e.FileUrl)
                    .HasColumnName("file_url");

                entity.Property(e => e.GeneratedAt)
                    .HasColumnName("generated_at")
                    .HasDefaultValueSql("NOW()");

                entity.Property(e => e.Schedule)
                    .HasColumnName("schedule");

                entity.HasOne(d => d.GeneratedBy)
                    .WithMany(p => p.Reports)
                    .HasForeignKey(d => d.GeneratedById);
            });
        }

        private void ConfigureLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Log>(entity =>
            {
                entity.ToTable("log");
                entity.HasKey(e => e.LogId);

                entity.Property(e => e.LogId)
                    .HasColumnName("log_id")
                    .ValueGeneratedNever();

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id");

                entity.Property(e => e.Action)
                    .HasColumnName("action");

                entity.Property(e => e.EntityType)
                    .HasColumnName("entity_type");

                entity.Property(e => e.EntityId)
                    .HasColumnName("entity_id");

                entity.Property(e => e.Metadata)
                    .HasColumnName("metadata")
                    .HasColumnType("jsonb");

                entity.Property(e => e.IpAddress)
                    .HasColumnName("ip_address");

                entity.Property(e => e.UserAgent)
                    .HasColumnName("user_agent");

                entity.Property(e => e.LoggedAt)
                    .HasColumnName("logged_at")
                    .HasDefaultValueSql("NOW()");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Logs)
                    .HasForeignKey(d => d.UserId);
            });
        }

        private void ConfigureOrganisationInvitation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrganisationInvitation>(entity =>
            {
                entity.ToTable("organisation_invitation");
                entity.HasKey(e => e.InvitationId);

                entity.Property(e => e.InvitationId)
                    .HasColumnName("invitation_id")
                    .ValueGeneratedNever();

                entity.Property(e => e.OrganisationId)
                    .HasColumnName("organisation_id")
                    .IsRequired();

                entity.Property(e => e.Email)
                    .HasColumnName("email")
                    .IsRequired();

                entity.Property(e => e.Token)
                    .HasColumnName("token")
                    .IsRequired();

                entity.Property(e => e.Role)
                    .HasColumnName("role");

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("NOW()");

                entity.Property(e => e.ExpiresAt)
                    .HasColumnName("expires_at")
                    .IsRequired();

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .IsRequired();

                entity.HasOne(d => d.Organisation)
                    .WithMany()
                    .HasForeignKey(d => d.OrganisationId);

                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId);

                entity.HasIndex(e => e.Token)
                    .IsUnique();
            });
        }
    }
} 