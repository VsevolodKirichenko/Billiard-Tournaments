using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tournaments.Api.Authentication;
using Tournaments.Api.Contracts;
using Tournaments.Core.Application;
using Tournaments.Core.Domain;

namespace Tournaments.Api.Controllers;

[ApiController, Route("trainings"), Authorize(Roles = Roles.ManageTrainings)]
public sealed class TrainingsController(IMapper mapper) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TrainingResponse>> Create(RecordTrainingRequest request,
        [FromServices] ICommandHandler<RecordTraining, TrainingSession> handler, CancellationToken ct)
    {
        var session = await handler.Handle(new(request.PlayerId, request.Exercise, request.PerformedAt,
            request.DurationMinutes, request.Attempts, request.SuccessfulAttempts, User.Actor()), ct);
        return Created("/trainings", mapper.Map<TrainingResponse>(session));
    }
    [HttpGet]
    public async Task<TrainingResponse[]> List([FromQuery] Guid playerId,
        [FromServices] IQueryHandler<GetTrainings, TrainingSession[]> handler, CancellationToken ct, [FromQuery] int take = 100) =>
        mapper.Map<TrainingResponse[]>(await handler.Handle(new(playerId, take), ct));
}
