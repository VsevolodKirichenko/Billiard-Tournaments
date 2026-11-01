namespace Tournaments.Core.Domain;

public sealed class Tournament
{
    private Tournament() { }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string OwnerId { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }
    public long Version { get; private set; }
    public List<Match> Matches { get; private set; } = [];

    public static Tournament Create(string name, string ownerId, IReadOnlyList<Guid> players)
    {
        if (players.Count is < 2 or > 32 || players.Contains(Guid.Empty) || players.Distinct().Count() != players.Count)
            throw new DomainException("Provide 2 to 32 distinct player IDs.");
        var tournament = new Tournament
        {
            Id = Guid.NewGuid(), Name = Text.Required(name, 120, "Tournament name"),
            OwnerId = Text.Required(ownerId, 450, "Owner"), CreatedAt = DateTime.UtcNow, Version = 1
        };
        for (var i = 0; i < players.Count; i++)
            for (var j = i + 1; j < players.Count; j++)
                tournament.Matches.Add(Match.Create(tournament.Id, players[i], players[j]));
        return tournament;
    }

    public bool SetResult(Guid matchId, int homeScore, int awayScore)
    {
        var match = Matches.SingleOrDefault(m => m.Id == matchId) ?? throw new NotFoundException("Match not found.");
        if (!match.SetResult(homeScore, awayScore)) return false;
        Version++;
        return true;
    }
}
