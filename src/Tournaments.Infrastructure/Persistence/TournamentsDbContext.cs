using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tournaments.Core.Domain;
using Tournaments.Infrastructure.Identity;
using Tournaments.Infrastructure.Persistence.Entities;

namespace Tournaments.Infrastructure.Persistence;

public sealed class TournamentsDbContext(DbContextOptions<TournamentsDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<TrainingSession> Trainings => Set<TrainingSession>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<TournamentStanding> Standings => Set<TournamentStanding>();
    public DbSet<StatisticsState> StatisticsStates => Set<StatisticsState>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<Player>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).HasMaxLength(80).IsRequired();
            b.HasIndex(p => p.Name);
        });
        model.Entity<Tournament>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.Name).HasMaxLength(120).IsRequired();
            b.Property(t => t.OwnerId).HasMaxLength(450).IsRequired();
            b.Property(t => t.Version).IsConcurrencyToken();
            b.HasIndex(t => t.CreatedAt);
            b.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.OwnerId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(t => t.Matches).WithOne().HasForeignKey(m => m.TournamentId);
        });
        model.Entity<Match>(b =>
        {
            b.HasKey(m => m.Id);
            b.Ignore(m => m.IsCompleted);
            b.HasIndex(m => new { m.TournamentId, m.HomeId, m.AwayId }).IsUnique();
            b.HasIndex(m => new { m.HomeId, m.TournamentId });
            b.HasIndex(m => new { m.AwayId, m.TournamentId });
            b.HasOne<Player>().WithMany().HasForeignKey(m => m.HomeId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Player>().WithMany().HasForeignKey(m => m.AwayId).OnDelete(DeleteBehavior.Restrict);
            b.ToTable(t => t.HasCheckConstraint("CK_Match_Score", "(\"HomeScore\" IS NULL AND \"AwayScore\" IS NULL) OR (\"HomeScore\" IS NOT NULL AND \"AwayScore\" IS NOT NULL AND \"HomeScore\" BETWEEN 0 AND 100 AND \"AwayScore\" BETWEEN 0 AND 100 AND \"HomeScore\" <> \"AwayScore\")"));
        });
        model.Entity<TrainingSession>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.Exercise).HasMaxLength(200).IsRequired();
            b.Property(t => t.OwnerId).HasMaxLength(450).IsRequired();
            b.HasIndex(t => new { t.PlayerId, t.PerformedAt });
            b.HasOne<Player>().WithMany().HasForeignKey(t => t.PlayerId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<OutboxMessage>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Payload).HasColumnType("jsonb").IsRequired();
            b.HasIndex(e => e.CreatedAt).HasFilter("\"PublishedAt\" IS NULL");
        });
        model.Entity<InboxMessage>().HasKey(e => e.Id);
        model.Entity<TournamentStanding>(b =>
        {
            b.HasKey(s => new { s.TournamentId, s.PlayerId });
            b.HasIndex(s => s.PlayerId);
            b.HasOne<Tournament>().WithMany().HasForeignKey(s => s.TournamentId);
            b.HasOne<Player>().WithMany().HasForeignKey(s => s.PlayerId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<StatisticsState>().HasData(new StatisticsState { Id = 1, Version = 0 });
    }
}
