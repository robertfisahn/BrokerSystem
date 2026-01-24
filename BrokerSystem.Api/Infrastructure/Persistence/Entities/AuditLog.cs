using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("audit_log")]
public partial class AuditLog
{
    [Key]
    [Column("log_id")]
    public int LogId { get; set; }

    [Column("table_name")]
    [StringLength(100)]
    public string TableName { get; set; } = null!;

    [Column("record_id")]
    public int RecordId { get; set; }

    [Column("action")]
    [StringLength(10)]
    public string Action { get; set; } = null!;

    [Column("old_value")]
    public string? OldValue { get; set; }

    [Column("new_value")]
    public string? NewValue { get; set; }

    [Column("changed_by_user_id")]
    public int? ChangedByUserId { get; set; }

    [Column("changed_at")]
    public DateTime ChangedAt { get; set; }

    [ForeignKey("ChangedByUserId")]
    [InverseProperty("AuditLogs")]
    public virtual User? ChangedByUser { get; set; }
}
