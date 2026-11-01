namespace Tournaments.Api.Authentication;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "billiard-api";
    public string Audience { get; set; } = "billiard-clients";
    public string Key { get; set; } = "";
    public int LifetimeMinutes { get; set; } = 60;
}
