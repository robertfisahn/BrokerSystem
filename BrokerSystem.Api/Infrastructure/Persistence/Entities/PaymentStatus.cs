using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("payment_statuses")]
[Index("StatusName", Name = "UQ_payment_statuses", IsUnique = true)]
public partial class PaymentStatus
{
    [Key]
    [Column("payment_status_id")]
    public int PaymentStatusId { get; set; }

    [Column("status_name")]
    [StringLength(50)]
    public string StatusName { get; set; } = null!;

    [InverseProperty("PaymentStatus")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
