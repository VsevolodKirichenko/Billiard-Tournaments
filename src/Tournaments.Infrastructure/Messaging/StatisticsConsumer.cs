using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tournaments.Core.Application;
using Tournaments.Core.Events;

namespace Tournaments.Infrastructure.Messaging;

public sealed class StatisticsConsumer(IServiceScopeFactory scopes, IOptions<KafkaOptions> options, ILogger<StatisticsConsumer> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers, GroupId = options.Value.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest, EnableAutoCommit = false, EnableAutoOffsetStore = false
        }).Build();
        consumer.Subscribe(options.Value.Topic);
        ConsumeResult<string, string>? current = null;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    current ??= consumer.Consume(TimeSpan.FromMilliseconds(500));
                    if (current is null) { await Task.Delay(50, stoppingToken); continue; }
                    var message = JsonSerializer.Deserialize<MatchResultChanged>(current.Message.Value)
                        ?? throw new JsonException("Empty match-result event.");
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IStatisticsProjector>().Process(message, stoppingToken);
                    consumer.Commit(current);
                    current = null;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    // Keep the same record until projection+commit succeeds. Malformed
                    // messages intentionally stop progress rather than being discarded.
                    logger.LogError(ex, "Statistics event failed; retrying without advancing offset");
                    await Task.Delay(2000, stoppingToken);
                }
            }
        }
        finally { consumer.Close(); }
    }
}
