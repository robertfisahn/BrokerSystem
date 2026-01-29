using BrokerSystem.Api.Features.Policies.GetPolicies;
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
}
