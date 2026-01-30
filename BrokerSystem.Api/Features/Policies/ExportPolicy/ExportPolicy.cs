using BrokerSystem.Api.Common.Exceptions;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BrokerSystem.Api.Features.Policies.ExportPolicy;

public record ExportPolicyQuery(int PolicyId) : IRequest<byte[]>;

public record PolicyExportDto(
    string PolicyNumber,
    string ClientFirstName,
    string ClientLastName,
    string? ClientCompanyName,
    string PolicyTypeName,
    decimal SumInsured,
    decimal PremiumAmount,
    DateTime StartDate,
    DateTime EndDate,
    string StatusName,
    string AgentName);

public class ExportPolicyHandler(BrokerSystemDbContext db) : IRequestHandler<ExportPolicyQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportPolicyQuery request, CancellationToken ct)
    {
        var policyDto = await db.Policies
            .Where(p => p.PolicyId == request.PolicyId)
            .Select(p => new PolicyExportDto(
                p.PolicyNumber,
                p.Client.FirstName ?? "",
                p.Client.LastName ?? "",
                p.Client.CompanyName,
                p.PolicyType.TypeName,
                p.SumInsured,
                p.PremiumAmount,
                p.StartDate.ToDateTime(TimeOnly.MinValue),
                p.EndDate.ToDateTime(TimeOnly.MinValue),
                p.Status.StatusName,
                (p.Agent.FirstName ?? "") + " " + (p.Agent.LastName ?? "")
            ))
            .FirstOrDefaultAsync(ct);

        if (policyDto == null)
        {
            throw new NotFoundException($"Polisa o ID {request.PolicyId} nie została znaleziona.");
        }

        var document = new PolicyDocument(policyDto);
        return document.GeneratePdf();
    }
}

public class PolicyDocument(PolicyExportDto policy) : IDocument
{
    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Margin(50);
                
                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Strona ");
                    x.CurrentPageNumber();
                });
            });
    }

    private void ComposeHeader(IContainer container)
    {
        var titleStyle = TextStyle.Default.FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text($"CERTYFIKAT POLISY #{policy.PolicyNumber}").Style(titleStyle);

                column.Item().Text(text =>
                {
                    text.Span("Data wystawienia: ").SemiBold();
                    text.Span($"{DateTime.Now:dd.MM.yyyy}");
                });
            });

            row.ConstantItem(100).Height(50).Placeholder();
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(40).Column(column =>
        {
            column.Spacing(20);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("UBEZPIECZYCIEL").SemiBold();
                    c.Item().Text("BrokerSystem Sp. z o.o.");
                    c.Item().Text("ul. Technologiczna 10");
                    c.Item().Text("00-001 Warszawa");
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("UBEZPIECZONY").SemiBold();
                    c.Item().Text($"{policy.ClientFirstName} {policy.ClientLastName}");
                    c.Item().Text(policy.ClientCompanyName ?? "");
                });
            });

            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(150);
                    columns.RelativeColumn();
                });

                table.Cell().Element(CellStyle).Text("Typ Polisy");
                table.Cell().Element(CellStyle).Text(policy.PolicyTypeName);

                table.Cell().Element(CellStyle).Text("Okres Ochrony");
                table.Cell().Element(CellStyle).Text($"{policy.StartDate:dd.MM.yyyy} - {policy.EndDate:dd.MM.yyyy}");

                table.Cell().Element(CellStyle).Text("Suma Ubezpieczenia");
                table.Cell().Element(CellStyle).Text($"{policy.SumInsured:N2} PLN");

                table.Cell().Element(CellStyle).Text("Składka Łączna");
                table.Cell().Element(CellStyle).Text($"{policy.PremiumAmount:N2} PLN").SemiBold();

                table.Cell().Element(CellStyle).Text("Status");
                table.Cell().Element(CellStyle).Text(policy.StatusName);

                static IContainer CellStyle(IContainer container)
                {
                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
            });

            column.Item().PaddingTop(20).Text(x =>
            {
                x.Span("Agent prowadzący: ").SemiBold();
                x.Span(policy.AgentName);
            });
        });
    }
}
