namespace Tournaments.Core.Events;

public sealed record MatchResultChanged(Guid EventId, Guid TournamentId, Guid MatchId,
    long TournamentVersion, DateTime OccurredAt);
