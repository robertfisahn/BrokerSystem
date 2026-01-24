using BrokerSystem.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Context;

public partial class BrokerSystemDbContext : DbContext
{
    public BrokerSystemDbContext(DbContextOptions<BrokerSystemDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Agent> Agents { get; set; }

    public virtual DbSet<AgentPerformance> AgentPerformances { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Claim> Claims { get; set; }

    public virtual DbSet<ClaimPayment> ClaimPayments { get; set; }

    public virtual DbSet<ClaimStatus> ClaimStatuses { get; set; }

    public virtual DbSet<ClaimStatusHistory> ClaimStatusHistories { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<ClientAddress> ClientAddresses { get; set; }

    public virtual DbSet<ClientContact> ClientContacts { get; set; }

    public virtual DbSet<ClientType> ClientTypes { get; set; }

    public virtual DbSet<Commission> Commissions { get; set; }

    public virtual DbSet<CommissionStatus> CommissionStatuses { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<PaymentStatus> PaymentStatuses { get; set; }

    public virtual DbSet<Policy> Policies { get; set; }

    public virtual DbSet<PolicyBeneficiary> PolicyBeneficiaries { get; set; }

    public virtual DbSet<PolicyCategory> PolicyCategories { get; set; }

    public virtual DbSet<PolicyStatus> PolicyStatuses { get; set; }

    public virtual DbSet<PolicyStatusHistory> PolicyStatusHistories { get; set; }

    public virtual DbSet<PolicyType> PolicyTypes { get; set; }

    public virtual DbSet<RiskAssessment> RiskAssessments { get; set; }

    public virtual DbSet<RiskLevel> RiskLevels { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.Property(e => e.CommissionRate).HasDefaultValueSql("((10.00))");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Manager).WithMany(p => p.InverseManager).HasConstraintName("FK_agents_manager");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(e => e.ChangedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.AuditLogs).HasConstraintName("FK_audit_log_users");
        });

        modelBuilder.Entity<Claim>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Policy).WithMany(p => p.Claims)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_claims_policies");

            entity.HasOne(d => d.Status).WithMany(p => p.Claims)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_claims_statuses");
        });

        modelBuilder.Entity<ClaimPayment>(entity =>
        {
            entity.HasOne(d => d.Claim).WithMany(p => p.ClaimPayments).HasConstraintName("FK_claim_payments_claims");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.ClaimPayments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_claim_payments_methods");
        });

        modelBuilder.Entity<ClaimStatusHistory>(entity =>
        {
            entity.Property(e => e.ChangedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Claim).WithMany(p => p.ClaimStatusHistories).HasConstraintName("FK_claim_status_history_claims");

            entity.HasOne(d => d.NewStatus).WithMany(p => p.ClaimStatusHistoryNewStatuses)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_claim_status_history_new_status");

            entity.HasOne(d => d.OldStatus).WithMany(p => p.ClaimStatusHistoryOldStatuses).HasConstraintName("FK_claim_status_history_old_status");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RegistrationDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.ClientType).WithMany(p => p.Clients)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clients_client_types");
        });

        modelBuilder.Entity<ClientAddress>(entity =>
        {
            entity.HasIndex(e => e.ClientId, "UX_client_addresses_current")
                .IsUnique()
                .HasFilter("([is_current]=(1))");

            entity.Property(e => e.Country).HasDefaultValue("Poland");
            entity.Property(e => e.IsCurrent).HasDefaultValue(true);
            entity.Property(e => e.ValidFrom).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Client).WithOne(p => p.ClientAddress).HasConstraintName("FK_client_addresses_clients");
        });

        modelBuilder.Entity<ClientContact>(entity =>
        {
            entity.HasIndex(e => e.ClientId, "UX_client_contacts_primary")
                .IsUnique()
                .HasFilter("([is_primary]=(1))");

            entity.HasOne(d => d.Client).WithOne(p => p.ClientContact).HasConstraintName("FK_client_contacts_clients");
        });

        modelBuilder.Entity<ClientType>(entity =>
        {
            entity.Property(e => e.DiscountRate).HasDefaultValueSql("((0.00))");
        });

        modelBuilder.Entity<Commission>(entity =>
        {
            entity.HasOne(d => d.Agent).WithMany(p => p.Commissions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_commissions_agents");

            entity.HasOne(d => d.CommissionStatus).WithMany(p => p.Commissions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_commissions_statuses");

            entity.HasOne(d => d.Policy).WithMany(p => p.Commissions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_commissions_policies");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasOne(d => d.Policy).WithMany(p => p.Invoices).HasConstraintName("FK_invoices_policies");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Payments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_payments_methods");

            entity.HasOne(d => d.PaymentStatus).WithMany(p => p.Payments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_payments_statuses");

            entity.HasOne(d => d.Policy).WithMany(p => p.Payments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_payments_policies");
        });

        modelBuilder.Entity<Policy>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Agent).WithMany(p => p.Policies)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_policies_agents");

            entity.HasOne(d => d.Client).WithMany(p => p.Policies)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_policies_clients");

            entity.HasOne(d => d.PolicyType).WithMany(p => p.Policies)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_policies_policy_types");

            entity.HasOne(d => d.Status).WithMany(p => p.Policies)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_policies_statuses");
        });

        modelBuilder.Entity<PolicyBeneficiary>(entity =>
        {
            entity.HasOne(d => d.Policy).WithMany(p => p.PolicyBeneficiaries).HasConstraintName("FK_policy_beneficiaries_policy");
        });

        modelBuilder.Entity<PolicyCategory>(entity =>
        {
            entity.Property(e => e.Level).HasDefaultValue(1);

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory).HasConstraintName("FK_policy_categories_parent");
        });

        modelBuilder.Entity<PolicyStatusHistory>(entity =>
        {
            entity.Property(e => e.ChangedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.NewStatus).WithMany(p => p.PolicyStatusHistoryNewStatuses)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_policy_status_history_new_status");

            entity.HasOne(d => d.OldStatus).WithMany(p => p.PolicyStatusHistoryOldStatuses).HasConstraintName("FK_policy_status_history_old_status");

            entity.HasOne(d => d.Policy).WithMany(p => p.PolicyStatusHistories).HasConstraintName("FK_policy_status_history_policy");
        });

        modelBuilder.Entity<PolicyType>(entity =>
        {
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Category).WithMany(p => p.PolicyTypes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_policy_types_categories");
        });

        modelBuilder.Entity<RiskAssessment>(entity =>
        {
            entity.Property(e => e.AssessmentDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Policy).WithMany(p => p.RiskAssessments).HasConstraintName("FK_risk_assessments_policies");

            entity.HasOne(d => d.RiskLevel).WithMany(p => p.RiskAssessments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_risk_assessments_levels");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Agent).WithMany(p => p.Users).HasConstraintName("FK_users_agents");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles).HasConstraintName("FK_user_roles_roles");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles).HasConstraintName("FK_user_roles_users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
