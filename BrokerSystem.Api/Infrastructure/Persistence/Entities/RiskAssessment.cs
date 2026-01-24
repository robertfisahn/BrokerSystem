using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("risk_assessments")]
public partial class RiskAssessment
{
    [Key]
    [Column("assessment_id")]
    public int AssessmentId { get; set; }

    [Column("policy_id")]
    public int PolicyId { get; set; }

    [Column("risk_level_id")]
    public int RiskLevelId { get; set; }

    [Column("assessment_date")]
    public DateOnly AssessmentDate { get; set; }

    [Column("assessed_by_user_id")]
    public int? AssessedByUserId { get; set; }

    [Column("score", TypeName = "decimal(5, 2)")]
    public decimal Score { get; set; }

    [Column("notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    [ForeignKey("PolicyId")]
    [InverseProperty("RiskAssessments")]
    public virtual Policy Policy { get; set; } = null!;

    [ForeignKey("RiskLevelId")]
    [InverseProperty("RiskAssessments")]
    public virtual RiskLevel RiskLevel { get; set; } = null!;
}
