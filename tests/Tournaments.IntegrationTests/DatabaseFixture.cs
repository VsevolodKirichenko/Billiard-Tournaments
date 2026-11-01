using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Tournaments.Infrastructure.Persistence;

namespace Tournaments.IntegrationTests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly RedisContainer redis = new RedisBuilder("redis:7.4-alpine").Build();
    public IConnectionMultiplexer Redis { get; private set; } = null!;
    public TournamentsDbContext CreateDb() => new(new DbContextOptionsBuilder<TournamentsDbContext>()
        .UseNpgsql(postgres.GetConnectionString()).Options);

    public async Task InitializeAsync()
    {
        await Task.WhenAll(postgres.StartAsync(), redis.StartAsync());
        Redis = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
    }
    public async Task DisposeAsync()
    {
        if (Redis is not null) await Redis.DisposeAsync();
        await postgres.DisposeAsync();
        await redis.DisposeAsync();
    }
}
