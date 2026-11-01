namespace Tournaments.Infrastructure.Persistence.Entities;

public sealed class TournamentStanding
{
    public Guid TournamentId { get; set; }
    public Guid PlayerId { get; set; }
    public int Played { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Scored { get; set; }
    public int Conceded { get; set; }
    public long SourceVersion { get; set; }
}
