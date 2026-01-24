using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Entities;

[Table("invoices")]
[Index("InvoiceNumber", Name = "UQ_invoices_number", IsUnique = true)]
public partial class Invoice
{
    [Key]
    [Column("invoice_id")]
    public int InvoiceId { get; set; }

    [Column("invoice_number")]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = null!;

    [Column("policy_id")]
    public int PolicyId { get; set; }

    [Column("issue_date")]
    public DateOnly IssueDate { get; set; }

    [Column("due_date")]
    public DateOnly DueDate { get; set; }

    [Column("total_net", TypeName = "decimal(10, 2)")]
    public decimal TotalNet { get; set; }

    [Column("vat_amount", TypeName = "decimal(10, 2)")]
    public decimal VatAmount { get; set; }

    [Column("total_gross", TypeName = "decimal(10, 2)")]
    public decimal TotalGross { get; set; }

    [Column("is_paid")]
    public bool IsPaid { get; set; }

    [ForeignKey("PolicyId")]
    [InverseProperty("Invoices")]
    public virtual Policy Policy { get; set; } = null!;
}
