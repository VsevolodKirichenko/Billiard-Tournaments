using Tournaments.Core.Domain;

namespace Tournaments.Core.Application;

public sealed class GetTournamentHandler(ITournamentRepository tournaments) : IQueryHandler<GetTournament, Tournament>
{
    public async Task<Tournament> Handle(GetTournament query, CancellationToken ct) =>
        await tournaments.Get(query.Id, ct) ?? throw new NotFoundException("Tournament not found.");
}
public sealed class ListTournamentsHandler(ITournamentRepository tournaments) : IQueryHandler<ListTournaments, Tournament[]>
{
    public Task<Tournament[]> Handle(ListTournaments query, CancellationToken ct) => tournaments.List(Math.Clamp(query.Take, 1, 100), ct);
}
public sealed class ListPlayersHandler(IPlayerRepository players) : IQueryHandler<ListPlayers, Player[]>
{
    public Task<Player[]> Handle(ListPlayers query, CancellationToken ct) => players.List(Math.Clamp(query.Take, 1, 200), ct);
}
public sealed class GetStandingsHandler(ITournamentRepository tournaments, IPlayerRepository players, IStandingsCache cache)
    : IQueryHandler<GetStandings, Standing[]>
{
    public async Task<Standing[]> Handle(GetStandings query, CancellationToken ct)
    {
        var tournament = await tournaments.Get(query.TournamentId, ct) ?? throw new NotFoundException("Tournament not found.");
        var cached = await cache.Get(tournament.Id, tournament.Version, ct);
        if (cached is not null) return cached;
        var ids = tournament.Matches.SelectMany(m => new[] { m.HomeId, m.AwayId }).Distinct().ToArray();
        var names = (await players.GetMany(ids, ct)).ToDictionary(p => p.Id, p => p.Name);
        var rows = Ranking.Calculate(tournament.Matches, names);
        await cache.Set(tournament.Id, tournament.Version, rows, ct);
        return rows;
    }
}
public sealed class GetLeaderboardHandler(IStatisticsReader reader) : IQueryHandler<GetLeaderboard, Leaderboard>
{
    public Task<Leaderboard> Handle(GetLeaderboard query, CancellationToken ct) => reader.GetLeaderboard(Math.Clamp(query.Take, 1, 200), ct);
}
public sealed class GetTrainingsHandler(ITrainingRepository trainings) : IQueryHandler<GetTrainings, TrainingSession[]>
{
    public Task<TrainingSession[]> Handle(GetTrainings query, CancellationToken ct) => trainings.List(query.PlayerId, Math.Clamp(query.Take, 1, 200), ct);
}
