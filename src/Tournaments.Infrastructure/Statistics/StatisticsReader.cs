using Microsoft.EntityFrameworkCore;
using Tournaments.Core.Application;
using Tournaments.Core.Domain;
using Tournaments.Infrastructure.Persistence;

namespace Tournaments.Infrastructure.Statistics;

public sealed class StatisticsReader(TournamentsDbContext db, ILeaderboardCache cache) : IStatisticsReader
{
    public async Task<Leaderboard> GetLeaderboard(int take, CancellationToken ct)
    {
        var version = await db.StatisticsStates.AsNoTracking().Where(s => s.Id == 1).Select(s => s.Version).SingleAsync(ct);
        var cached = await cache.Get(version, take, ct);
        if (cached is not null) return cached;
        var totals = db.Standings.AsNoTracking().GroupBy(s => s.PlayerId).Select(g => new
        {
            PlayerId = g.Key, Played = g.Sum(s => s.Played), Wins = g.Sum(s => s.Wins), Losses = g.Sum(s => s.Losses),
            Scored = g.Sum(s => s.Scored), Conceded = g.Sum(s => s.Conceded)
        });
        var rows = await (from total in totals join player in db.Players on total.PlayerId equals player.Id
            orderby total.Wins descending, (total.Scored - total.Conceded) descending, total.Scored descending, player.Name, player.Id
            select new Standing(player.Id, player.Name, total.Played, total.Wins, total.Losses, total.Scored, total.Conceded))
            .Take(take).ToArrayAsync(ct);
        var result = new Leaderboard(version, rows);
        await cache.Set(version, take, result, ct);
        return result;
    }
}
