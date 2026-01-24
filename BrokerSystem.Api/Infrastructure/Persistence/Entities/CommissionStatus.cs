using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("commission_statuses")]
[Index("StatusName", Name = "UQ_commission_statuses", IsUnique = true)]
public partial class CommissionStatus
{
    [Key]
    [Column("commission_status_id")]
    public int CommissionStatusId { get; set; }

    [Column("status_name")]
    [StringLength(50)]
    public string StatusName { get; set; } = null!;

    [InverseProperty("CommissionStatus")]
    public virtual ICollection<Commission> Commissions { get; set; } = new List<Commission>();
}
