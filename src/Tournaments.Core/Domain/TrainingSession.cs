namespace Tournaments.Core.Domain;

public sealed class TrainingSession
{
    private TrainingSession() { }
    public Guid Id { get; private set; }
    public Guid PlayerId { get; private set; }
    public string OwnerId { get; private set; } = "";
    public string Exercise { get; private set; } = "";
    public DateTime PerformedAt { get; private set; }
    public int DurationMinutes { get; private set; }
    public int Attempts { get; private set; }
    public int SuccessfulAttempts { get; private set; }

    public static TrainingSession Create(Guid playerId, string ownerId, string exercise,
        DateTime performedAt, int durationMinutes, int attempts, int successfulAttempts)
    {
        if (playerId == Guid.Empty || performedAt.Kind != DateTimeKind.Utc || durationMinutes is < 1 or > 1440 ||
            attempts is < 1 or > 100000 || successfulAttempts < 0 || successfulAttempts > attempts)
            throw new DomainException("Invalid training: use UTC time, positive duration/attempts and successes <= attempts.");
        return new()
        {
            Id = Guid.NewGuid(), PlayerId = playerId, OwnerId = Text.Required(ownerId, 450, "Owner"),
            Exercise = Text.Required(exercise, 200, "Exercise"), PerformedAt = performedAt,
            DurationMinutes = durationMinutes, Attempts = attempts, SuccessfulAttempts = successfulAttempts
        };
    }
}
