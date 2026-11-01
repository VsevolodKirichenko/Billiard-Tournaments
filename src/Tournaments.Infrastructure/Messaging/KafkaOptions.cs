namespace Tournaments.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public bool Enabled { get; set; } = true;
    public string BootstrapServers { get; set; } = "127.0.0.1:9092";
    public string Topic { get; set; } = "billiard.match-results.v1";
    public string GroupId { get; set; } = "billiard-statistics-v1";
}
