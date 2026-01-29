using BrokerSystem.Api.Features.Policies.CreatePolicy;
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
    public async Task<IActionResult> GetPolicies([FromQuery] GetPoliciesQuery query)
    {
        var result = await mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("lookups")]
    public async Task<IActionResult> GetLookups()
    {
        var result = await mediator.Send(new GetPolicyLookupsQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePolicyCommand command)
    {
        var id = await mediator.Send(command);
        return Ok(id);
    }
}
