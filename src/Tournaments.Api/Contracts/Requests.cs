using System.ComponentModel.DataAnnotations;

namespace Tournaments.Api.Contracts;

public sealed record CreatePlayerRequest([Required, StringLength(80, MinimumLength = 1)] string Name);
public sealed record CreateTournamentRequest([Required, StringLength(120, MinimumLength = 1)] string Name,
    [Required, MinLength(2), MaxLength(32)] Guid[] Players);
public sealed record SubmitResultRequest([Range(0, 100)] int HomeScore, [Range(0, 100)] int AwayScore,
    [Range(1, long.MaxValue)] long ExpectedVersion);
public sealed record RecordTrainingRequest(Guid PlayerId, [Required, StringLength(200)] string Exercise,
    DateTime PerformedAt, [Range(1, 1440)] int DurationMinutes, [Range(1, 100000)] int Attempts, int SuccessfulAttempts);
public sealed record RegisterRequest([Required, EmailAddress] string Email, [Required, MinLength(10)] string Password);
public sealed record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public sealed record AssignRoleRequest([Required] string Role);
