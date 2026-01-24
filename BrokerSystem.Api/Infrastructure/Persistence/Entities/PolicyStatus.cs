using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("policy_statuses")]
[Index("StatusName", Name = "UQ_policy_statuses_name", IsUnique = true)]
public partial class PolicyStatus
{
    [Key]
    [Column("status_id")]
    public int StatusId { get; set; }

    [Column("status_name")]
    [StringLength(50)]
    public string StatusName { get; set; } = null!;

    [Column("description")]
    [StringLength(255)]
    public string? Description { get; set; }

    [Column("is_active_policy")]
    public bool IsActivePolicy { get; set; }

    [InverseProperty("Status")]
    public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();

    [InverseProperty("NewStatus")]
    public virtual ICollection<PolicyStatusHistory> PolicyStatusHistoryNewStatuses { get; set; } = new List<PolicyStatusHistory>();

    [InverseProperty("OldStatus")]
    public virtual ICollection<PolicyStatusHistory> PolicyStatusHistoryOldStatuses { get; set; } = new List<PolicyStatusHistory>();
}
