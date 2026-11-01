using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Tournaments.Core.Application;
using Tournaments.Core.Domain;

namespace Tournaments.Infrastructure.Caching;

public sealed class RedisCache(IConnectionMultiplexer redis, ILogger<RedisCache> logger)
    : IStandingsCache, ILeaderboardCache
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    public Task<Standing[]?> Get(Guid tournamentId, long version, CancellationToken ct) =>
        Read<Standing[]>($"tournament:{tournamentId}:v{version}:standings", ct);
    public Task Set(Guid tournamentId, long version, Standing[] rows, CancellationToken ct) =>
        Write($"tournament:{tournamentId}:v{version}:standings", rows, ct);
    public Task<Leaderboard?> Get(long version, int take, CancellationToken ct) => Read<Leaderboard>($"leaderboard:v{version}:n{take}", ct);
    public Task Set(long version, int take, Leaderboard result, CancellationToken ct) => Write($"leaderboard:v{version}:n{take}", result, ct);

    private async Task<T?> Read<T>(string key, CancellationToken ct)
    {
        try
        {
            var value = await redis.GetDatabase().StringGetAsync(key).WaitAsync(ct);
            return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex) when (ex is RedisException or JsonException)
        {
            logger.LogWarning(ex, "Cache read failed; falling back to PostgreSQL");
            return default;
        }
    }
    private async Task Write<T>(string key, T value, CancellationToken ct)
    {
        try { await redis.GetDatabase().StringSetAsync(key, JsonSerializer.Serialize(value), Lifetime).WaitAsync(ct); }
        catch (RedisException ex) { logger.LogWarning(ex, "Cache write failed"); }
    }
}
