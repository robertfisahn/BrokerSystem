using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("policies")]
[Index("AgentId", Name = "IX_policies_agent")]
[Index("ClientId", Name = "IX_policies_client")]
[Index("PolicyNumber", Name = "UQ_policies_number", IsUnique = true)]
public partial class Policy
{
    [Key]
    [Column("policy_id")]
    public int PolicyId { get; set; }

    [Column("policy_number")]
    [StringLength(50)]
    public string PolicyNumber { get; set; } = null!;

    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("policy_type_id")]
    public int PolicyTypeId { get; set; }

    [Column("agent_id")]
    public int AgentId { get; set; }

    [Column("status_id")]
    public int StatusId { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly EndDate { get; set; }

    [Column("premium_amount", TypeName = "decimal(10, 2)")]
    public decimal PremiumAmount { get; set; }

    [Column("sum_insured", TypeName = "decimal(12, 2)")]
    public decimal SumInsured { get; set; }

    [Column("payment_frequency")]
    [StringLength(20)]
    public string PaymentFrequency { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("AgentId")]
    [InverseProperty("Policies")]
    public virtual Agent Agent { get; set; } = null!;

    [InverseProperty("Policy")]
    public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();

    [ForeignKey("ClientId")]
    [InverseProperty("Policies")]
    public virtual Client Client { get; set; } = null!;

    [InverseProperty("Policy")]
    public virtual ICollection<Commission> Commissions { get; set; } = new List<Commission>();

    [InverseProperty("Policy")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    [InverseProperty("Policy")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [InverseProperty("Policy")]
    public virtual ICollection<PolicyBeneficiary> PolicyBeneficiaries { get; set; } = new List<PolicyBeneficiary>();

    [InverseProperty("Policy")]
    public virtual ICollection<PolicyStatusHistory> PolicyStatusHistories { get; set; } = new List<PolicyStatusHistory>();

    [ForeignKey("PolicyTypeId")]
    [InverseProperty("Policies")]
    public virtual PolicyType PolicyType { get; set; } = null!;

    [InverseProperty("Policy")]
    public virtual ICollection<RiskAssessment> RiskAssessments { get; set; } = new List<RiskAssessment>();

    [ForeignKey("StatusId")]
    [InverseProperty("Policies")]
    public virtual PolicyStatus Status { get; set; } = null!;
}
