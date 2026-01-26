using BrokerSystem.Api.Features.Clients.GetClients;
using BrokerSystem.Api.Features.Clients.GetClientsStats;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using BrokerSystem.Api.Features.Clients.GetClient360;

namespace BrokerSystem.Api.Features.Clients
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController(IMediator mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetClients(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string sortBy = "clientId",
            [FromQuery] bool sortDescending = false,
            CancellationToken ct = default)
        {
            var query = new GetClientsQuery(page, pageSize, search, sortBy, sortDescending);
            var result = await mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetClientsStats(CancellationToken ct)
        {
            var result = await mediator.Send(new GetClientsStatsQuery(), ct);
            return Ok(result);
        }

        [HttpGet("{id}/360")]
        public async Task<IActionResult> GetClient360(int id, CancellationToken ct)
        {
            var result = await mediator.Send(new GetClient360Query(id), ct);
            if (result == null) return NotFound();
            return Ok(result);
        }
    }
}
