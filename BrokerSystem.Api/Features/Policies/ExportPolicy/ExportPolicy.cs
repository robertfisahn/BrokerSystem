using BrokerSystem.Api.Common.Exceptions;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure.Persistence;
using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using BrokerSystem.Api.Common.Endpoints;

namespace BrokerSystem.Api.Features.Policies.ExportPolicy;

public class ExportPolicyEndpoint : IEndpointDefinition
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("api/policies/{id:int}/export", async (int id, IMediator mediator) => 
        {
            var pdf = await mediator.Send(new ExportPolicyQuery(id));
            return Results.File(pdf, "application/pdf", $"Polisa_{id}.pdf");
        })
        .WithName("ExportPolicy")
        .WithTags("Policies");
    }
}

public record ExportPolicyQuery(int PolicyId) : IRequest<byte[]>;

public class PolicyExportDto
{
    public string PolicyNumber { get; set; } = string.Empty;
    public string ClientFirstName { get; set; } = string.Empty;
    public string ClientLastName { get; set; } = string.Empty;
    public string? ClientCompanyName { get; set; }
    public string PolicyTypeName { get; set; } = string.Empty;
    public decimal SumInsured { get; set; }
    public decimal PremiumAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
}

public class ExportPolicyHandler(BrokerSystemDbContext db) : IRequestHandler<ExportPolicyQuery, byte[]>
{
    /// <summary>
    /// Builds the SQL query for policy export, joining clients, types, statuses, and agents.
    /// </summary>
    public static string GetExportSql(ISqlDialect sqlDialect) => $@"
            SELECT 
                p.policy_number AS PolicyNumber,
                c.first_name AS ClientFirstName,
                c.last_name AS ClientLastName,
                c.company_name AS ClientCompanyName,
                pt.type_name AS PolicyTypeName,
                p.sum_insured AS SumInsured,
                p.premium_amount AS PremiumAmount,
                p.start_date AS StartDate,
                p.end_date AS EndDate,
                ps.status_name AS StatusName,
                {sqlDialect.Concat("a.first_name", "' '", "a.last_name")} AS AgentName
            FROM policies p
            JOIN clients c ON p.client_id = c.client_id
            JOIN policy_types pt ON p.policy_type_id = pt.policy_type_id
            JOIN policy_statuses ps ON p.status_id = ps.status_id
            JOIN agents a ON p.agent_id = a.agent_id
            WHERE p.policy_id = @PolicyId";

    public async Task<byte[]> Handle(ExportPolicyQuery request, CancellationToken ct)
    {
        using var connection = db.Database.GetDbConnection();
        var sqlDialect = db.Database.Sql();

        var sql = GetExportSql(sqlDialect);

        var policyDto = await connection.QueryFirstOrDefaultAsync<PolicyExportDto>(sql, new { PolicyId = request.PolicyId });

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
