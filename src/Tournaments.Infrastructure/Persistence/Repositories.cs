using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tournaments.Core.Application;
using Tournaments.Core.Domain;
using Tournaments.Core.Events;
using Tournaments.Infrastructure.Persistence.Entities;

namespace Tournaments.Infrastructure.Persistence;

public sealed class TournamentRepository(TournamentsDbContext db) : ITournamentRepository
{
    public Task<Tournament?> Get(Guid id, CancellationToken ct) => db.Tournaments.Include(t => t.Matches).SingleOrDefaultAsync(t => t.Id == id, ct);
    public Task<Tournament[]> List(int take, CancellationToken ct) => db.Tournaments.AsNoTracking().Include(t => t.Matches)
        .OrderByDescending(t => t.CreatedAt).Take(take).ToArrayAsync(ct);
    public async Task Add(Tournament tournament, CancellationToken ct) => await db.Tournaments.AddAsync(tournament, ct);
}
public sealed class PlayerRepository(TournamentsDbContext db) : IPlayerRepository
{
    public Task<Player[]> GetMany(Guid[] ids, CancellationToken ct) => db.Players.Where(p => ids.Contains(p.Id)).ToArrayAsync(ct);
    public Task<Player[]> List(int take, CancellationToken ct) => db.Players.AsNoTracking().OrderBy(p => p.Name).ThenBy(p => p.Id).Take(take).ToArrayAsync(ct);
    public async Task Add(Player player, CancellationToken ct) => await db.Players.AddAsync(player, ct);
}
public sealed class TrainingRepository(TournamentsDbContext db) : ITrainingRepository
{
    public async Task Add(TrainingSession session, CancellationToken ct) => await db.Trainings.AddAsync(session, ct);
    public Task<TrainingSession[]> List(Guid playerId, int take, CancellationToken ct) => db.Trainings.AsNoTracking()
        .Where(t => t.PlayerId == playerId).OrderByDescending(t => t.PerformedAt).Take(take).ToArrayAsync(ct);
}
public sealed class UnitOfWork(TournamentsDbContext db) : IUnitOfWork
{
    public async Task Save(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConflictException("Concurrent update. Reload the tournament and retry."); }
    }
}
public sealed class Outbox(TournamentsDbContext db) : IOutbox
{
    public void Add(MatchResultChanged message) => db.OutboxMessages.Add(new OutboxMessage
    {
        Id = message.EventId, TournamentId = message.TournamentId, CreatedAt = message.OccurredAt,
        Payload = JsonSerializer.Serialize(message)
    });
}
