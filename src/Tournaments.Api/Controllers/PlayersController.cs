using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tournaments.Api.Authentication;
using Tournaments.Api.Contracts;
using Tournaments.Core.Application;
using Tournaments.Core.Domain;

namespace Tournaments.Api.Controllers;

[ApiController, Route("players")]
public sealed class PlayersController(IMapper mapper) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<PlayerResponse[]>> List([FromServices] IQueryHandler<ListPlayers, Player[]> handler,
        CancellationToken ct, [FromQuery] int take = 100) => mapper.Map<PlayerResponse[]>(await handler.Handle(new(take), ct));

    [HttpPost, Authorize(Roles = Roles.ManageTournaments)]
    public async Task<ActionResult<PlayerResponse>> Create(CreatePlayerRequest request,
        [FromServices] ICommandHandler<CreatePlayer, Player> handler, CancellationToken ct)
    {
        var player = await handler.Handle(new(request.Name), ct);
        return Created("/players", mapper.Map<PlayerResponse>(player));
    }
}
