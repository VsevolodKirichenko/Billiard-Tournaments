using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tournaments.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TournamentsDbContext>
{
    public TournamentsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<TournamentsDbContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__Postgres") ??
            "Host=127.0.0.1;Port=55432;Database=billiards;Username=billiards;Password=local-billiards-only").Options);
}
