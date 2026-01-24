using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("policy_beneficiaries")]
public partial class PolicyBeneficiary
{
    [Key]
    [Column("beneficiary_id")]
    public int BeneficiaryId { get; set; }

    [Column("policy_id")]
    public int PolicyId { get; set; }

    [Column("first_name")]
    [StringLength(100)]
    public string FirstName { get; set; } = null!;

    [Column("last_name")]
    [StringLength(100)]
    public string LastName { get; set; } = null!;

    [Column("relationship")]
    [StringLength(50)]
    public string Relationship { get; set; } = null!;

    [Column("share_percentage", TypeName = "decimal(5, 2)")]
    public decimal SharePercentage { get; set; }

    [ForeignKey("PolicyId")]
    [InverseProperty("PolicyBeneficiaries")]
    public virtual Policy Policy { get; set; } = null!;
}
