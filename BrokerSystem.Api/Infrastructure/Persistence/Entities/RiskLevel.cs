using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("risk_levels")]
[Index("LevelName", Name = "UQ_risk_levels_name", IsUnique = true)]
public partial class RiskLevel
{
    [Key]
    [Column("risk_level_id")]
    public int RiskLevelId { get; set; }

    [Column("level_name")]
    [StringLength(50)]
    public string LevelName { get; set; } = null!;

    [Column("premium_multiplier", TypeName = "decimal(4, 2)")]
    public decimal PremiumMultiplier { get; set; }

    [InverseProperty("RiskLevel")]
    public virtual ICollection<RiskAssessment> RiskAssessments { get; set; } = new List<RiskAssessment>();
}
