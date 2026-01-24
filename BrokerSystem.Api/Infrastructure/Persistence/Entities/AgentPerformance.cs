using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("agent_performance")]
[Index("AgentId", "Year", "Month", Name = "UQ_agent_performance", IsUnique = true)]
public partial class AgentPerformance
{
    [Key]
    [Column("performance_id")]
    public int PerformanceId { get; set; }

    [Column("agent_id")]
    public int AgentId { get; set; }

    [Column("year")]
    public int Year { get; set; }

    [Column("month")]
    public int Month { get; set; }

    [Column("policies_sold")]
    public int PoliciesSold { get; set; }

    [Column("total_premium", TypeName = "decimal(12, 2)")]
    public decimal TotalPremium { get; set; }

    [Column("total_commission", TypeName = "decimal(10, 2)")]
    public decimal TotalCommission { get; set; }

    [Column("customer_satisfaction_score", TypeName = "decimal(3, 2)")]
    public decimal? CustomerSatisfactionScore { get; set; }
}
