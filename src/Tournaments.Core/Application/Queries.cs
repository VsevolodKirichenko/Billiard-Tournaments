namespace Tournaments.Core.Application;

public sealed record GetTournament(Guid Id);
public sealed record ListTournaments(int Take = 50);
public sealed record ListPlayers(int Take = 100);
public sealed record GetStandings(Guid TournamentId);
public sealed record GetLeaderboard(int Take = 100);
public sealed record GetTrainings(Guid PlayerId, int Take = 100);
