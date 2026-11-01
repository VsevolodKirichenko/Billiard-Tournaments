using Microsoft.EntityFrameworkCore;
using Tournaments.Core.Application;
using Tournaments.Core.Domain;
using Tournaments.Core.Events;
using Tournaments.Infrastructure.Persistence;
using Tournaments.Infrastructure.Persistence.Entities;

namespace Tournaments.Infrastructure.Statistics;

public sealed class StatisticsProjector(TournamentsDbContext db) : IStatisticsProjector
{
    public async Task Process(MatchResultChanged message, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Serialize projection updates across consumer instances. Inbox and projection
        // are committed together: a redelivery never adds a win twice.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(8610051301)", ct);
        if (await db.InboxMessages.AnyAsync(e => e.Id == message.EventId, ct)) return;
        var tournament = await db.Tournaments.AsNoTracking().Include(t => t.Matches)
            .SingleOrDefaultAsync(t => t.Id == message.TournamentId, ct)
            ?? throw new InvalidOperationException("Event references a missing tournament.");
        var ids = tournament.Matches.SelectMany(m => new[] { m.HomeId, m.AwayId }).Distinct().ToArray();
        var names = await db.Players.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);
        // Rebuild from the source of truth, so edits and out-of-order events are safe.
        var rows = Ranking.Calculate(tournament.Matches, names);
        await db.Standings.Where(s => s.TournamentId == tournament.Id).ExecuteDeleteAsync(ct);
        db.Standings.AddRange(rows.Select(row => new TournamentStanding
        {
            TournamentId = tournament.Id, PlayerId = row.PlayerId, Played = row.Played, Wins = row.Wins,
            Losses = row.Losses, Scored = row.Scored, Conceded = row.Conceded, SourceVersion = tournament.Version
        }));
        db.InboxMessages.Add(new InboxMessage { Id = message.EventId, ProcessedAt = DateTime.UtcNow });
        var state = await db.StatisticsStates.SingleAsync(s => s.Id == 1, ct);
        state.Version++;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
