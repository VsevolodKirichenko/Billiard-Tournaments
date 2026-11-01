using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tournaments.Core.Application;

namespace Tournaments.Api.Controllers;

[ApiController, Route("statistics")]
public sealed class StatisticsController : ControllerBase
{
    // The global read model is eventually consistent. Tournament standings are immediate.
    [HttpGet("leaderboard"), AllowAnonymous]
    public Task<Leaderboard> Leaderboard([FromServices] IQueryHandler<GetLeaderboard, Leaderboard> handler,
        CancellationToken ct, [FromQuery] int take = 100) => handler.Handle(new(take), ct);
}
