using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tournaments.Core.Application;
using Tournaments.Core.Domain;
using Tournaments.Core.Events;
using Tournaments.Infrastructure.Caching;
using Tournaments.Infrastructure.Identity;
using Tournaments.Infrastructure.Persistence;
using Tournaments.Infrastructure.Statistics;

namespace Tournaments.IntegrationTests;

public sealed class PersistenceTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task ProjectionIsIdempotentAndRedisUsesTheCurrentTournamentVersion()
    {
        var cache = new RedisCache(fixture.Redis, NullLogger<RedisCache>.Instance);
        var tournament = await Seed();
        MatchResultChanged message;
        await using (var db = fixture.CreateDb())
        {
            var query = new GetStandingsHandler(new TournamentRepository(db), new PlayerRepository(db), cache);
            Assert.All(await query.Handle(new(tournament.Id), default), s => Assert.Equal(0, s.Wins));
            var handler = Handler(db);
            await handler.Handle(new(tournament.Id, tournament.Matches[0].Id, 8, 3, 1, new(tournament.OwnerId, false)), default);
            var outbox = await db.OutboxMessages.SingleAsync(e => e.TournamentId == tournament.Id);
            message = JsonSerializer.Deserialize<MatchResultChanged>(outbox.Payload)!;
        }
        await using (var db = fixture.CreateDb())
        {
            var query = new GetStandingsHandler(new TournamentRepository(db), new PlayerRepository(db), cache);
            Assert.Equal(1, (await query.Handle(new(tournament.Id), default))[0].Wins);
        }
        await using (var db = fixture.CreateDb()) await new StatisticsProjector(db).Process(message, default);
        await using (var db = fixture.CreateDb()) await new StatisticsProjector(db).Process(message, default);
        await using (var db = fixture.CreateDb())
        {
            Assert.Equal(1, await db.InboxMessages.CountAsync(e => e.Id == message.EventId));
            Assert.Equal(1, await db.Standings.Where(s => s.TournamentId == tournament.Id).SumAsync(s => s.Wins));
            var reader = new StatisticsReader(db, cache);
            Assert.Contains((await reader.GetLeaderboard(100, default)).Players, s => s.PlayerId == tournament.Matches[0].HomeId && s.Wins == 1);
        }
    }

    [Fact]
    public async Task ConcurrentCommandsDoNotLoseResultsOrLeavePhantomOutboxEvents()
    {
        var tournament = await Seed();
        await using var first = fixture.CreateDb();
        await using var second = fixture.CreateDb();
        await new TournamentRepository(first).Get(tournament.Id, default);
        await new TournamentRepository(second).Get(tournament.Id, default);
        var match = tournament.Matches.Single();
        await Handler(first).Handle(new(tournament.Id, match.Id, 8, 2, 1, new(tournament.OwnerId, false)), default);
        await Assert.ThrowsAsync<ConflictException>(() => Handler(second).Handle(new(tournament.Id, match.Id, 3, 8, 1, new(tournament.OwnerId, false)), default));
        await using var verify = fixture.CreateDb();
        Assert.Equal(1, await verify.OutboxMessages.CountAsync(e => e.TournamentId == tournament.Id));
        Assert.Equal(8, (await verify.Matches.SingleAsync(m => m.Id == match.Id)).HomeScore);
    }

    private static SubmitResultHandler Handler(TournamentsDbContext db) => new(new TournamentRepository(db), new Outbox(db), new UnitOfWork(db));
    private async Task<Tournament> Seed()
    {
        await using var db = fixture.CreateDb();
        var owner = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "test-" + Guid.NewGuid() };
        db.Users.Add(owner);
        var a = Player.Create("Player A"); var b = Player.Create("Player B");
        db.Players.AddRange(a, b);
        var tournament = Tournament.Create("Integration Cup", owner.Id, [a.Id, b.Id]);
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();
        return tournament;
    }
}
