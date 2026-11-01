namespace Tournaments.Api.Contracts;

public sealed record PlayerResponse(Guid Id, string Name);
public sealed record MatchResponse(Guid Id, Guid HomeId, Guid AwayId, int? HomeScore, int? AwayScore);
public sealed record TournamentResponse(Guid Id, string Name, string OwnerId, long Version, DateTime CreatedAt, MatchResponse[] Matches);
public sealed record TrainingResponse(Guid Id, Guid PlayerId, string Exercise, DateTime PerformedAt, int DurationMinutes,
    int Attempts, int SuccessfulAttempts);
public sealed record TokenResponse(string AccessToken, DateTime ExpiresAt);
