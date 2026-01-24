using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("policy_types")]
public partial class PolicyType
{
    [Key]
    [Column("policy_type_id")]
    public int PolicyTypeId { get; set; }

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("type_name")]
    [StringLength(100)]
    public string TypeName { get; set; } = null!;

    [Column("base_premium", TypeName = "decimal(10, 2)")]
    public decimal BasePremium { get; set; }

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [ForeignKey("CategoryId")]
    [InverseProperty("PolicyTypes")]
    public virtual PolicyCategory Category { get; set; } = null!;

    [InverseProperty("PolicyType")]
    public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();
}
