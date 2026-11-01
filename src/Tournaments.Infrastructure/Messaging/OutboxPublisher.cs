using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tournaments.Infrastructure.Persistence;

namespace Tournaments.Infrastructure.Messaging;

public sealed class OutboxPublisher(IServiceScopeFactory scopes, IOptions<KafkaOptions> options, ILogger<OutboxPublisher> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers, EnableIdempotence = true,
            Acks = Acks.All, MessageTimeoutMs = 10000
        }).Build();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TournamentsDbContext>();
                await using var transaction = await db.Database.BeginTransactionAsync(stoppingToken);
                var batch = await db.OutboxMessages.FromSqlRaw("""
                    SELECT * FROM "OutboxMessages" WHERE "PublishedAt" IS NULL
                    ORDER BY "CreatedAt", "Id" LIMIT 20 FOR UPDATE SKIP LOCKED
                    """).ToArrayAsync(stoppingToken);
                foreach (var message in batch)
                {
                    await producer.ProduceAsync(options.Value.Topic, new Message<string, string>
                    {
                        Key = message.TournamentId.ToString(), Value = message.Payload
                    }, stoppingToken);
                    message.PublishedAt = DateTime.UtcNow;
                }
                await db.SaveChangesAsync(stoppingToken);
                await transaction.CommitAsync(stoppingToken);
                if (batch.Length == 0) await Task.Delay(500, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Outbox publication failed; pending events will be retried");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
