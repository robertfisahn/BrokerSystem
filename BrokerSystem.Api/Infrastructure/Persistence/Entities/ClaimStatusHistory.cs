using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("claim_status_history")]
public partial class ClaimStatusHistory
{
    [Key]
    [Column("history_id")]
    public int HistoryId { get; set; }

    [Column("claim_id")]
    public int ClaimId { get; set; }

    [Column("old_status_id")]
    public int? OldStatusId { get; set; }

    [Column("new_status_id")]
    public int NewStatusId { get; set; }

    [Column("changed_at")]
    public DateTime ChangedAt { get; set; }

    [Column("changed_by_user_id")]
    public int? ChangedByUserId { get; set; }

    [Column("notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    [ForeignKey("ClaimId")]
    [InverseProperty("ClaimStatusHistories")]
    public virtual Claim Claim { get; set; } = null!;

    [ForeignKey("NewStatusId")]
    [InverseProperty("ClaimStatusHistoryNewStatuses")]
    public virtual ClaimStatus NewStatus { get; set; } = null!;

    [ForeignKey("OldStatusId")]
    [InverseProperty("ClaimStatusHistoryOldStatuses")]
    public virtual ClaimStatus? OldStatus { get; set; }
}
