using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tournaments.Api.Authentication;
using Tournaments.Api.Contracts;
using Tournaments.Core.Application;
using Tournaments.Core.Domain;

namespace Tournaments.Api.Controllers;

[ApiController, Route("tournaments")]
public sealed class TournamentsController(IMapper mapper) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<TournamentResponse[]>> List([FromServices] IQueryHandler<ListTournaments, Tournament[]> handler,
        CancellationToken ct, [FromQuery] int take = 50) => mapper.Map<TournamentResponse[]>(await handler.Handle(new(take), ct));

    [HttpPost, Authorize(Roles = Roles.ManageTournaments)]
    public async Task<ActionResult<TournamentResponse>> Create(CreateTournamentRequest request,
        [FromServices] ICommandHandler<CreateTournament, Tournament> handler, CancellationToken ct)
    {
        var tournament = await handler.Handle(new(request.Name, request.Players, User.Actor()), ct);
        return CreatedAtAction(nameof(Get), new { id = tournament.Id }, mapper.Map<TournamentResponse>(tournament));
    }
    [HttpGet("{id:guid}"), AllowAnonymous]
    public async Task<ActionResult<TournamentResponse>> Get(Guid id, [FromServices] IQueryHandler<GetTournament, Tournament> handler,
        CancellationToken ct) => mapper.Map<TournamentResponse>(await handler.Handle(new(id), ct));

    [HttpPut("{id:guid}/matches/{matchId:guid}/result"), Authorize(Roles = Roles.ManageTournaments)]
    public async Task<ActionResult<TournamentResponse>> Result(Guid id, Guid matchId, SubmitResultRequest request,
        [FromServices] ICommandHandler<SubmitResult, Tournament> handler, CancellationToken ct) =>
        mapper.Map<TournamentResponse>(await handler.Handle(new(id, matchId, request.HomeScore, request.AwayScore,
            request.ExpectedVersion, User.Actor()), ct));

    [HttpGet("{id:guid}/standings"), AllowAnonymous]
    public async Task<Standing[]> Standings(Guid id, [FromServices] IQueryHandler<GetStandings, Standing[]> handler,
        CancellationToken ct) => await handler.Handle(new(id), ct);
}
