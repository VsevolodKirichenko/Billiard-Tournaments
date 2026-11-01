namespace Tournaments.Core.Application;

public sealed record CreatePlayer(string Name);
public sealed record CreateTournament(string Name, Guid[] Players, Actor Actor);
public sealed record SubmitResult(Guid TournamentId, Guid MatchId, int HomeScore, int AwayScore, long ExpectedVersion, Actor Actor);
public sealed record RecordTraining(Guid PlayerId, string Exercise, DateTime PerformedAt, int DurationMinutes,
    int Attempts, int SuccessfulAttempts, Actor Actor);
