using Tournaments.Core.Application;
using Tournaments.Core.Domain;

namespace Tournaments.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreatePlayer, Player>, CreatePlayerHandler>();
        services.AddScoped<ICommandHandler<CreateTournament, Tournament>, CreateTournamentHandler>();
        services.AddScoped<ICommandHandler<SubmitResult, Tournament>, SubmitResultHandler>();
        services.AddScoped<ICommandHandler<RecordTraining, TrainingSession>, RecordTrainingHandler>();
        services.AddScoped<IQueryHandler<GetTournament, Tournament>, GetTournamentHandler>();
        services.AddScoped<IQueryHandler<ListTournaments, Tournament[]>, ListTournamentsHandler>();
        services.AddScoped<IQueryHandler<ListPlayers, Player[]>, ListPlayersHandler>();
        services.AddScoped<IQueryHandler<GetStandings, Standing[]>, GetStandingsHandler>();
        services.AddScoped<IQueryHandler<GetLeaderboard, Leaderboard>, GetLeaderboardHandler>();
        services.AddScoped<IQueryHandler<GetTrainings, TrainingSession[]>, GetTrainingsHandler>();
        return services;
    }
}
