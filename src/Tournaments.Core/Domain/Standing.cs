namespace Tournaments.Core.Domain;

public sealed record Standing(Guid PlayerId, string Name, int Played, int Wins, int Losses, int Scored, int Conceded)
{
    public int Difference => Scored - Conceded;
}

public static class Ranking
{
    public static Standing[] Calculate(IEnumerable<Match> matches, IReadOnlyDictionary<Guid, string> players)
    {
        var totals = players.ToDictionary(p => p.Key, p => new Standing(p.Key, p.Value, 0, 0, 0, 0, 0));
        foreach (var match in matches.Where(m => m.IsCompleted))
        {
            Apply(match.HomeId, match.HomeScore!.Value, match.AwayScore!.Value);
            Apply(match.AwayId, match.AwayScore!.Value, match.HomeScore!.Value);
        }
        return totals.Values.OrderByDescending(s => s.Wins).ThenByDescending(s => s.Difference)
            .ThenByDescending(s => s.Scored).ThenBy(s => s.Name, StringComparer.Ordinal).ToArray();

        void Apply(Guid playerId, int scored, int conceded)
        {
            var row = totals[playerId];
            totals[playerId] = row with
            {
                Played = row.Played + 1, Wins = row.Wins + (scored > conceded ? 1 : 0),
                Losses = row.Losses + (scored < conceded ? 1 : 0), Scored = row.Scored + scored,
                Conceded = row.Conceded + conceded
            };
        }
    }
}
