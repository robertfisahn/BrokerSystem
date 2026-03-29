using BrokerSystem.Api.Common.Auth;
using BrokerSystem.Api.Common.Exceptions;
using BrokerSystem.Api.Infrastructure.Hubs;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure.Persistence.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using BrokerSystem.Api.Common.Endpoints;

namespace BrokerSystem.Api.Features.Policies.CreatePolicy;

public class CreatePolicyEndpoint : IEndpointDefinition
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("api/policies", async (CreatePolicyCommand command, IMediator mediator) =>
                Results.Ok(await mediator.Send(command)))
            .WithName("CreatePolicy")
            .WithTags("Policies");
    }
}

public record CreatePolicyCommand(
    string PolicyNumber,
    int ClientId,
    int PolicyTypeId,
    int AgentId,
    decimal PremiumAmount,
    decimal SumInsured,
    DateTime StartDate,
    DateTime EndDate,
    string PaymentFrequency) : IRequest<int>, IAuthorizeableRequest;

public class CreatePolicyValidator : AbstractValidator<CreatePolicyCommand>
{
    public CreatePolicyValidator()
    {
        RuleFor(x => x.PolicyNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ClientId).GreaterThan(0);
        RuleFor(x => x.PolicyTypeId).GreaterThan(0);
        RuleFor(x => x.AgentId).GreaterThan(0);
        RuleFor(x => x.PremiumAmount).GreaterThan(0);
        RuleFor(x => x.SumInsured).GreaterThan(0);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty().GreaterThan(x => x.StartDate);
        RuleFor(x => x.PaymentFrequency).NotEmpty();
    }
}

public class CreatePolicyHandler(
    BrokerSystemDbContext db,
    IHubContext<BrokerHub> hub,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreatePolicyCommand, int>
{
    public async Task<int> Handle(CreatePolicyCommand request, CancellationToken ct)
    {
        // Izolacja danych / Bezpieczeństwo: 
        // Tylko Agenci mogą wystawiać polisy. To jest uprawnienie specyficzne dla roli.
        if (!currentUserService.IsAgent)
        {
            throw new ForbidException("Tylko Agenci mogą wystawiać polisy. Twoja rola nie posiada tych uprawnień.");
        }

        if (!currentUserService.AgentId.HasValue)
        {
            throw new BadRequestException(
                "Twój profil Agenta nie został odnaleziony. Skontaktuj się z Administratorem.");
        }

        var effectiveAgentId = currentUserService.AgentId.Value;

        if (await db.Policies.AnyAsync(p => p.PolicyNumber == request.PolicyNumber, ct))
        {
            throw new BadRequestException("Polisa o takim numerze już istnieje.");
        }

        if (!await db.Clients.AnyAsync(c => c.ClientId == request.ClientId, ct))
        {
            throw new NotFoundException($"Klient o ID {request.ClientId} nie istnieje.");
        }

        var activeStatus = await db.PolicyStatuses
                               .FirstOrDefaultAsync(s => s.StatusName == "Active", ct)
                           ?? await db.PolicyStatuses.FirstAsync(ct);

        var policy = new Policy
        {
            PolicyNumber = request.PolicyNumber,
            ClientId = request.ClientId,
            PolicyTypeId = request.PolicyTypeId,
            AgentId = effectiveAgentId,
            StatusId = activeStatus.StatusId,
            StartDate = DateOnly.FromDateTime(request.StartDate),
            EndDate = DateOnly.FromDateTime(request.EndDate),
            PremiumAmount = request.PremiumAmount,
            SumInsured = request.SumInsured,
            PaymentFrequency = request.PaymentFrequency,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Policies.Add(policy);
        await db.SaveChangesAsync(ct);

        await hub.Clients.All.SendAsync("ReceiveNotification",
            "Nowa Polisa",
            $"Utworzono polisę o numerze {policy.PolicyNumber}",
            ct);

        return policy.PolicyId;
    }
}

