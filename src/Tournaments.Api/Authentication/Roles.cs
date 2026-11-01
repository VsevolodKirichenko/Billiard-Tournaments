namespace Tournaments.Api.Authentication;

public static class Roles
{
    public const string Administrator = "Administrator";
    public const string Organizer = "Organizer";
    public const string Trainer = "Trainer";
    public const string Player = "Player";
    public const string ManageTournaments = Administrator + "," + Organizer;
    public const string ManageTrainings = Administrator + "," + Trainer;
    public static readonly string[] All = [Administrator, Organizer, Trainer, Player];
}
