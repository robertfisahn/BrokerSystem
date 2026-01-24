using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("policy_categories")]
public partial class PolicyCategory
{
    [Key]
    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("parent_category_id")]
    public int? ParentCategoryId { get; set; }

    [Column("category_name")]
    [StringLength(100)]
    public string CategoryName { get; set; } = null!;

    [Column("level")]
    public int Level { get; set; }

    [Column("path")]
    [StringLength(500)]
    public string? Path { get; set; }

    [InverseProperty("ParentCategory")]
    public virtual ICollection<PolicyCategory> InverseParentCategory { get; set; } = new List<PolicyCategory>();

    [ForeignKey("ParentCategoryId")]
    [InverseProperty("InverseParentCategory")]
    public virtual PolicyCategory? ParentCategory { get; set; }

    [InverseProperty("Category")]
    public virtual ICollection<PolicyType> PolicyTypes { get; set; } = new List<PolicyType>();
}
