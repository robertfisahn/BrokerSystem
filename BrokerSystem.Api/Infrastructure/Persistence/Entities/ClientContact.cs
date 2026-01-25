using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("client_contacts")]
[Index("ClientId", "ContactType", "ContactValue", Name = "UX_client_contacts_unique", IsUnique = true)]
public partial class ClientContact
{
    [Key]
    [Column("contact_id")]
    public int ContactId { get; set; }

    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("contact_type")]
    [StringLength(20)]
    public string ContactType { get; set; } = null!;

    [Column("contact_value")]
    [StringLength(255)]
    public string ContactValue { get; set; } = null!;

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    [ForeignKey("ClientId")]
    [InverseProperty("ClientContacts")]
    public virtual Client Client { get; set; } = null!;
}
