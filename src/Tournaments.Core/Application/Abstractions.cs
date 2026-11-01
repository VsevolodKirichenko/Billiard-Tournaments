using Tournaments.Core.Domain;
using Tournaments.Core.Events;

namespace Tournaments.Core.Application;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> Handle(TCommand command, CancellationToken ct);
}
public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken ct);
}
public interface ITournamentRepository
{
    Task<Tournament?> Get(Guid id, CancellationToken ct);
    Task<Tournament[]> List(int take, CancellationToken ct);
    Task Add(Tournament tournament, CancellationToken ct);
}
public interface IPlayerRepository
{
    Task<Player[]> GetMany(Guid[] ids, CancellationToken ct);
    Task<Player[]> List(int take, CancellationToken ct);
    Task Add(Player player, CancellationToken ct);
}
public interface ITrainingRepository
{
    Task Add(TrainingSession session, CancellationToken ct);
    Task<TrainingSession[]> List(Guid playerId, int take, CancellationToken ct);
}
public interface IUnitOfWork
{
    Task Save(CancellationToken ct);
}
public interface IOutbox
{
    void Add(MatchResultChanged message);
}
public interface IStandingsCache
{
    Task<Standing[]?> Get(Guid tournamentId, long version, CancellationToken ct);
    Task Set(Guid tournamentId, long version, Standing[] rows, CancellationToken ct);
}
public interface IStatisticsReader
{
    Task<Leaderboard> GetLeaderboard(int take, CancellationToken ct);
}
public interface IStatisticsProjector
{
    Task Process(MatchResultChanged message, CancellationToken ct);
}
public interface ILeaderboardCache
{
    Task<Leaderboard?> Get(long version, int take, CancellationToken ct);
    Task Set(long version, int take, Leaderboard result, CancellationToken ct);
}
public sealed record Leaderboard(long Version, Standing[] Players);
public sealed record Actor(string Id, bool IsAdministrator);
