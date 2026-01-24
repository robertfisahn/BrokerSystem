using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("clients")]
public partial class Client
{
    [Key]
    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("client_type_id")]
    public int ClientTypeId { get; set; }

    [Column("first_name")]
    [StringLength(100)]
    public string? FirstName { get; set; }

    [Column("last_name")]
    [StringLength(100)]
    public string? LastName { get; set; }

    [Column("company_name")]
    [StringLength(200)]
    public string? CompanyName { get; set; }

    [Column("tax_id")]
    [StringLength(20)]
    public string? TaxId { get; set; }

    [Column("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Column("registration_date")]
    public DateOnly RegistrationDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("risk_score", TypeName = "decimal(5, 2)")]
    public decimal? RiskScore { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Client")]
    public virtual ClientAddress? ClientAddress { get; set; }

    [InverseProperty("Client")]
    public virtual ClientContact? ClientContact { get; set; }

    [ForeignKey("ClientTypeId")]
    [InverseProperty("Clients")]
    public virtual ClientType ClientType { get; set; } = null!;

    [InverseProperty("Client")]
    public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();
}
