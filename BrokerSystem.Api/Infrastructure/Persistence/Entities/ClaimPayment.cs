using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("claim_payments")]
public partial class ClaimPayment
{
    [Key]
    [Column("payment_id")]
    public int PaymentId { get; set; }

    [Column("claim_id")]
    public int ClaimId { get; set; }

    [Column("amount", TypeName = "decimal(10, 2)")]
    public decimal Amount { get; set; }

    [Column("payment_date")]
    public DateOnly PaymentDate { get; set; }

    [Column("payment_method_id")]
    public int PaymentMethodId { get; set; }

    [Column("reference_number")]
    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [ForeignKey("ClaimId")]
    [InverseProperty("ClaimPayments")]
    public virtual Claim Claim { get; set; } = null!;

    [ForeignKey("PaymentMethodId")]
    [InverseProperty("ClaimPayments")]
    public virtual PaymentMethod PaymentMethod { get; set; } = null!;
}
