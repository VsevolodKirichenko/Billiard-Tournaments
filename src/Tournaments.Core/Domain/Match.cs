namespace Tournaments.Core.Domain;

public sealed class Match
{
    private Match() { }
    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Guid HomeId { get; private set; }
    public Guid AwayId { get; private set; }
    public int? HomeScore { get; private set; }
    public int? AwayScore { get; private set; }
    public bool IsCompleted => HomeScore.HasValue;

    internal static Match Create(Guid tournamentId, Guid home, Guid away) => new()
    {
        Id = Guid.NewGuid(), TournamentId = tournamentId, HomeId = home, AwayId = away
    };

    internal bool SetResult(int home, int away)
    {
        if (home < 0 || away < 0 || home > 100 || away > 100 || home == away)
            throw new DomainException("Scores must be between 0 and 100; draws are not supported.");
        if (HomeScore == home && AwayScore == away) return false;
        HomeScore = home;
        AwayScore = away;
        return true;
    }
}
