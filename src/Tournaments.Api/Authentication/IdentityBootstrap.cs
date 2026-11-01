using Microsoft.AspNetCore.Identity;
using Tournaments.Infrastructure.Identity;

namespace Tournaments.Api.Authentication;

public static class IdentityBootstrap
{
    public static async Task Initialize(IServiceProvider services, IConfiguration config)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
            if (!await roles.RoleExistsAsync(role)) Check(await roles.CreateAsync(new IdentityRole(role)));
        var email = config["Bootstrap:AdminEmail"];
        var password = config["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email };
            Check(await users.CreateAsync(user, password));
        }
        if (!await users.IsInRoleAsync(user, Roles.Administrator)) Check(await users.AddToRoleAsync(user, Roles.Administrator));
    }
    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
