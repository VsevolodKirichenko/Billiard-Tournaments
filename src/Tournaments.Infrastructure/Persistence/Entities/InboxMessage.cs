namespace Tournaments.Infrastructure.Persistence.Entities;

public sealed class InboxMessage
{
    public Guid Id { get; set; }
    public DateTime ProcessedAt { get; set; }
}
