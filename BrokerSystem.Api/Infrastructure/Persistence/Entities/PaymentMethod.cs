using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("payment_methods")]
[Index("MethodName", Name = "UQ_payment_methods_name", IsUnique = true)]
public partial class PaymentMethod
{
    [Key]
    [Column("method_id")]
    public int MethodId { get; set; }

    [Column("method_name")]
    [StringLength(50)]
    public string MethodName { get; set; } = null!;

    [InverseProperty("PaymentMethod")]
    public virtual ICollection<ClaimPayment> ClaimPayments { get; set; } = new List<ClaimPayment>();

    [InverseProperty("PaymentMethod")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
