using Tournaments.Core.Domain;
using Tournaments.Core.Events;

namespace Tournaments.Core.Application;

public sealed class CreatePlayerHandler(IPlayerRepository players, IUnitOfWork uow) : ICommandHandler<CreatePlayer, Player>
{
    public async Task<Player> Handle(CreatePlayer command, CancellationToken ct)
    {
        var player = Player.Create(command.Name);
        await players.Add(player, ct);
        await uow.Save(ct);
        return player;
    }
}

public sealed class CreateTournamentHandler(ITournamentRepository tournaments, IPlayerRepository players, IUnitOfWork uow)
    : ICommandHandler<CreateTournament, Tournament>
{
    public async Task<Tournament> Handle(CreateTournament command, CancellationToken ct)
    {
        var tournament = Tournament.Create(command.Name, command.Actor.Id, command.Players);
        if ((await players.GetMany(command.Players, ct)).Length != command.Players.Length)
            throw new NotFoundException("One or more players do not exist.");
        await tournaments.Add(tournament, ct);
        await uow.Save(ct);
        return tournament;
    }
}

public sealed class SubmitResultHandler(ITournamentRepository tournaments, IOutbox outbox, IUnitOfWork uow)
    : ICommandHandler<SubmitResult, Tournament>
{
    public async Task<Tournament> Handle(SubmitResult command, CancellationToken ct)
    {
        var tournament = await tournaments.Get(command.TournamentId, ct) ?? throw new NotFoundException("Tournament not found.");
        if (tournament.OwnerId != command.Actor.Id && !command.Actor.IsAdministrator)
            throw new UnauthorizedAccessException("Only the tournament owner or administrator can change results.");
        if (tournament.Version != command.ExpectedVersion)
            throw new ConflictException("Tournament changed. Reload its current version before submitting a result.");
        if (tournament.SetResult(command.MatchId, command.HomeScore, command.AwayScore))
        {
            outbox.Add(new(Guid.NewGuid(), tournament.Id, command.MatchId, tournament.Version, DateTime.UtcNow));
            // Repository and outbox share one UnitOfWork: one atomic DB commit.
            await uow.Save(ct);
        }
        return tournament;
    }
}

public sealed class RecordTrainingHandler(IPlayerRepository players, ITrainingRepository trainings, IUnitOfWork uow)
    : ICommandHandler<RecordTraining, TrainingSession>
{
    public async Task<TrainingSession> Handle(RecordTraining command, CancellationToken ct)
    {
        if ((await players.GetMany([command.PlayerId], ct)).Length != 1) throw new NotFoundException("Player not found.");
        var session = TrainingSession.Create(command.PlayerId, command.Actor.Id, command.Exercise, command.PerformedAt,
            command.DurationMinutes, command.Attempts, command.SuccessfulAttempts);
        await trainings.Add(session, ct);
        await uow.Save(ct);
        return session;
    }
}
