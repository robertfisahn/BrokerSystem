using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("user_activities")]
public class UserActivity
{
    [Key]
    [Column("activity_id")]
    public int ActivityId { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("activity_type")]
    [MaxLength(50)]
    public string ActivityType { get; set; } = string.Empty;

    [Required]
    [Column("description")]
    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    [Column("entity_name")]
    [MaxLength(50)]
    public string? EntityName { get; set; }

    [Column("entity_id")]
    public string? EntityId { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("ip_address")]
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
