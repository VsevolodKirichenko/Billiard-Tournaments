using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tournaments.Api.Contracts;
using Tournaments.Infrastructure.Identity;

namespace Tournaments.Api.Authentication;

public sealed class TokenService(UserManager<ApplicationUser> users, IOptions<JwtOptions> options)
{
    public async Task<TokenResponse> Issue(ApplicationUser user)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) };
        claims.AddRange((await users.GetRolesAsync(user)).Select(role => new Claim("role", role)));
        var expires = DateTime.UtcNow.AddMinutes(options.Value.LifetimeMinutes);
        var token = new JwtSecurityToken(options.Value.Issuer, options.Value.Audience, claims,
            expires: expires, signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Key)), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
