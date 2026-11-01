namespace Tournaments.Api.Configuration;

public static class LocalDevelopmentConfiguration
{
    public static void AddLocalSecrets(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment()) return;
        var file = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", ".env"));
        if (!File.Exists(file)) return;
        var map = new Dictionary<string, string>
        {
            ["JWT_KEY"] = "Jwt:Key", ["ADMIN_EMAIL"] = "Bootstrap:AdminEmail", ["ADMIN_PASSWORD"] = "Bootstrap:AdminPassword"
        };
        foreach (var line in File.ReadLines(file))
        {
            var pair = line.Split('=', 2);
            if (pair.Length == 2 && map.TryGetValue(pair[0], out var key) && string.IsNullOrEmpty(builder.Configuration[key]))
                builder.Configuration[key] = pair[1];
        }
    }
}
