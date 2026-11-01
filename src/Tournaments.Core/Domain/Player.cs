namespace Tournaments.Core.Domain;

public sealed class Player
{
    private Player() { }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }

    public static Player Create(string name) => new()
    {
        Id = Guid.NewGuid(), Name = Text.Required(name, 80, "Player name"), CreatedAt = DateTime.UtcNow
    };
}

internal static class Text
{
    public static string Required(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new DomainException($"{label} must contain 1 to {maxLength} characters.");
        return value.Trim();
    }
}
