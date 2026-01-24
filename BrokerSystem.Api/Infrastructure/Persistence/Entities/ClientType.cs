using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("client_types")]
[Index("TypeName", Name = "UQ_client_types_type_name", IsUnique = true)]
public partial class ClientType
{
    [Key]
    [Column("client_type_id")]
    public int ClientTypeId { get; set; }

    [Column("type_name")]
    [StringLength(50)]
    public string TypeName { get; set; } = null!;

    [Column("description")]
    [StringLength(255)]
    public string? Description { get; set; }

    [Column("discount_rate", TypeName = "decimal(5, 2)")]
    public decimal DiscountRate { get; set; }

    [InverseProperty("ClientType")]
    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
}
