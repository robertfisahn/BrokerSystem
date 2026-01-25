using BrokerSystem.Api.Features.Clients.GetClients;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace BrokerSystem.Api.Features.Clients
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController(IMediator mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetClients(CancellationToken ct)
        {
            var result = await mediator.Send(new GetClientsQuery(), ct);
            return Ok(result);
        }
    }
}
