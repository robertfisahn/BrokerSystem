using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("client_addresses")]
public partial class ClientAddress
{
    [Key]
    [Column("address_id")]
    public int AddressId { get; set; }

    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("address_type")]
    [StringLength(20)]
    public string AddressType { get; set; } = null!;

    [Column("street")]
    [StringLength(200)]
    public string Street { get; set; } = null!;

    [Column("city")]
    [StringLength(100)]
    public string City { get; set; } = null!;

    [Column("postal_code")]
    [StringLength(10)]
    public string PostalCode { get; set; } = null!;

    [Column("country")]
    [StringLength(100)]
    public string Country { get; set; } = null!;

    [Column("valid_from")]
    public DateOnly ValidFrom { get; set; }

    [Column("valid_to")]
    public DateOnly? ValidTo { get; set; }

    [Column("is_current")]
    public bool IsCurrent { get; set; }

    [ForeignKey("ClientId")]
    [InverseProperty("ClientAddress")]
    public virtual Client Client { get; set; } = null!;
}
