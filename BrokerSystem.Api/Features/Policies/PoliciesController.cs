using BrokerSystem.Api.Features.Policies.CreatePolicy;
using BrokerSystem.Api.Features.Policies.ExportPolicy;
using BrokerSystem.Api.Features.Policies.GetPolicies;
using BrokerSystem.Api.Features.Policies.GetPolicyLookups;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BrokerSystem.Api.Features.Policies;

[ApiController]
[Route("api/[controller]")]
public class PoliciesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPolicies([FromQuery] GetPoliciesQuery query) => Ok(await mediator.Send(query));

    [HttpGet("lookups")]
    public async Task<IActionResult> GetLookups() => Ok(await mediator.Send(new GetPolicyLookupsQuery()));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePolicyCommand command) => Ok(await mediator.Send(command));

    [HttpGet("{id}/export")]
    public async Task<IActionResult> Export(int id) 
        => File(await mediator.Send(new ExportPolicyQuery(id)), "application/pdf", $"Polisa_{id}.pdf");
}
