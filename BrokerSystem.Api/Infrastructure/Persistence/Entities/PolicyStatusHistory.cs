using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("policy_status_history")]
public partial class PolicyStatusHistory
{
    [Key]
    [Column("history_id")]
    public int HistoryId { get; set; }

    [Column("policy_id")]
    public int PolicyId { get; set; }

    [Column("old_status_id")]
    public int? OldStatusId { get; set; }

    [Column("new_status_id")]
    public int NewStatusId { get; set; }

    [Column("changed_at")]
    public DateTime ChangedAt { get; set; }

    [Column("changed_by_user_id")]
    public int? ChangedByUserId { get; set; }

    [Column("reason")]
    [StringLength(500)]
    public string? Reason { get; set; }

    [ForeignKey("NewStatusId")]
    [InverseProperty("PolicyStatusHistoryNewStatuses")]
    public virtual PolicyStatus NewStatus { get; set; } = null!;

    [ForeignKey("OldStatusId")]
    [InverseProperty("PolicyStatusHistoryOldStatuses")]
    public virtual PolicyStatus? OldStatus { get; set; }

    [ForeignKey("PolicyId")]
    [InverseProperty("PolicyStatusHistories")]
    public virtual Policy Policy { get; set; } = null!;
}
