using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("agents")]
[Index("ManagerId", Name = "IX_agents_manager")]
[Index("Email", Name = "UQ_agents_email", IsUnique = true)]
public partial class Agent
{
    [Key]
    [Column("agent_id")]
    public int AgentId { get; set; }

    [Column("first_name")]
    [StringLength(100)]
    public string FirstName { get; set; } = null!;

    [Column("last_name")]
    [StringLength(100)]
    public string LastName { get; set; } = null!;

    [Column("email")]
    [StringLength(255)]
    public string Email { get; set; } = null!;

    [Column("phone")]
    [StringLength(20)]
    public string Phone { get; set; } = null!;

    [Column("manager_id")]
    public int? ManagerId { get; set; }

    [Column("hire_date")]
    public DateOnly HireDate { get; set; }

    [Column("commission_rate", TypeName = "decimal(5, 2)")]
    public decimal CommissionRate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [InverseProperty("Agent")]
    public virtual ICollection<Commission> Commissions { get; set; } = new List<Commission>();

    [InverseProperty("Manager")]
    public virtual ICollection<Agent> InverseManager { get; set; } = new List<Agent>();

    [ForeignKey("ManagerId")]
    [InverseProperty("InverseManager")]
    public virtual Agent? Manager { get; set; }

    [InverseProperty("Agent")]
    public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();

    [InverseProperty("Agent")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
