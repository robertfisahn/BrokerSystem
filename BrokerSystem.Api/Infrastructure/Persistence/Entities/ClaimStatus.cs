using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("claim_statuses")]
[Index("StatusName", Name = "UQ_claim_statuses_name", IsUnique = true)]
public partial class ClaimStatus
{
    [Key]
    [Column("status_id")]
    public int StatusId { get; set; }

    [Column("status_name")]
    [StringLength(50)]
    public string StatusName { get; set; } = null!;

    [Column("is_final")]
    public bool IsFinal { get; set; }

    [InverseProperty("NewStatus")]
    public virtual ICollection<ClaimStatusHistory> ClaimStatusHistoryNewStatuses { get; set; } = new List<ClaimStatusHistory>();

    [InverseProperty("OldStatus")]
    public virtual ICollection<ClaimStatusHistory> ClaimStatusHistoryOldStatuses { get; set; } = new List<ClaimStatusHistory>();

    [InverseProperty("Status")]
    public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
}
