using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("payments")]
[Index("PolicyId", Name = "IX_payments_policy")]
public partial class Payment
{
    [Key]
    [Column("payment_id")]
    public int PaymentId { get; set; }

    [Column("policy_id")]
    public int PolicyId { get; set; }

    [Column("amount", TypeName = "decimal(10, 2)")]
    public decimal Amount { get; set; }

    [Column("payment_date")]
    public DateOnly PaymentDate { get; set; }

    [Column("payment_method_id")]
    public int PaymentMethodId { get; set; }

    [Column("payment_status_id")]
    public int PaymentStatusId { get; set; }

    [Column("transaction_id")]
    [StringLength(100)]
    public string? TransactionId { get; set; }

    [ForeignKey("PaymentMethodId")]
    [InverseProperty("Payments")]
    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    [ForeignKey("PaymentStatusId")]
    [InverseProperty("Payments")]
    public virtual PaymentStatus PaymentStatus { get; set; } = null!;

    [ForeignKey("PolicyId")]
    [InverseProperty("Payments")]
    public virtual Policy Policy { get; set; } = null!;
}
