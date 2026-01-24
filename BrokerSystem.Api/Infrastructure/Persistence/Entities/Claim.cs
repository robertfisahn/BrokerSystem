using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("claims")]
[Index("PolicyId", Name = "IX_claims_policy")]
[Index("ClaimNumber", Name = "UQ_claims_number", IsUnique = true)]
public partial class Claim
{
    [Key]
    [Column("claim_id")]
    public int ClaimId { get; set; }

    [Column("claim_number")]
    [StringLength(50)]
    public string ClaimNumber { get; set; } = null!;

    [Column("policy_id")]
    public int PolicyId { get; set; }

    [Column("status_id")]
    public int StatusId { get; set; }

    [Column("incident_date")]
    public DateOnly IncidentDate { get; set; }

    [Column("reported_date")]
    public DateOnly ReportedDate { get; set; }

    [Column("claimed_amount", TypeName = "decimal(10, 2)")]
    public decimal ClaimedAmount { get; set; }

    [Column("approved_amount", TypeName = "decimal(10, 2)")]
    public decimal? ApprovedAmount { get; set; }

    [Column("description")]
    [StringLength(1000)]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("Claim")]
    public virtual ICollection<ClaimPayment> ClaimPayments { get; set; } = new List<ClaimPayment>();

    [InverseProperty("Claim")]
    public virtual ICollection<ClaimStatusHistory> ClaimStatusHistories { get; set; } = new List<ClaimStatusHistory>();

    [ForeignKey("PolicyId")]
    [InverseProperty("Claims")]
    public virtual Policy Policy { get; set; } = null!;

    [ForeignKey("StatusId")]
    [InverseProperty("Claims")]
    public virtual ClaimStatus Status { get; set; } = null!;
}
