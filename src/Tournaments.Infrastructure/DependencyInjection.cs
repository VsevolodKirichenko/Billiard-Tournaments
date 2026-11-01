using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Tournaments.Core.Application;
using Tournaments.Infrastructure.Caching;
using Tournaments.Infrastructure.Messaging;
using Tournaments.Infrastructure.Persistence;
using Tournaments.Infrastructure.Statistics;

namespace Tournaments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<TournamentsDbContext>(options => options.UseNpgsql(
            config.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Configure ConnectionStrings:Postgres.")));
        services.AddScoped<ITournamentRepository, TournamentRepository>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<ITrainingRepository, TrainingRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutbox, Outbox>();
        services.AddScoped<IStatisticsProjector, StatisticsProjector>();
        services.AddScoped<IStatisticsReader, StatisticsReader>();
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(
            ConfigurationOptions.Parse(config.GetConnectionString("Redis") ?? "127.0.0.1:6379,abortConnect=false,connectTimeout=2000,asyncTimeout=2000")));
        services.AddSingleton<RedisCache>();
        services.AddSingleton<IStandingsCache>(s => s.GetRequiredService<RedisCache>());
        services.AddSingleton<ILeaderboardCache>(s => s.GetRequiredService<RedisCache>());
        services.Configure<KafkaOptions>(config.GetSection("Kafka"));
        services.AddHostedService<OutboxPublisher>();
        services.AddHostedService<StatisticsConsumer>();
        return services;
    }
}
