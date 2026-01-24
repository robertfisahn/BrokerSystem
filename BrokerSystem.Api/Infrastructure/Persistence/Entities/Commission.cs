using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("commissions")]
public partial class Commission
{
    [Key]
    [Column("commission_id")]
    public int CommissionId { get; set; }

    [Column("policy_id")]
    public int PolicyId { get; set; }

    [Column("agent_id")]
    public int AgentId { get; set; }

    [Column("commission_rate", TypeName = "decimal(5, 2)")]
    public decimal CommissionRate { get; set; }

    [Column("commission_amount", TypeName = "decimal(10, 2)")]
    public decimal CommissionAmount { get; set; }

    [Column("payment_date")]
    public DateOnly? PaymentDate { get; set; }

    [Column("commission_status_id")]
    public int CommissionStatusId { get; set; }

    [ForeignKey("AgentId")]
    [InverseProperty("Commissions")]
    public virtual Agent Agent { get; set; } = null!;

    [ForeignKey("CommissionStatusId")]
    [InverseProperty("Commissions")]
    public virtual CommissionStatus CommissionStatus { get; set; } = null!;

    [ForeignKey("PolicyId")]
    [InverseProperty("Commissions")]
    public virtual Policy Policy { get; set; } = null!;
}
