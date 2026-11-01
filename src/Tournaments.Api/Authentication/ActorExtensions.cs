using System.Security.Claims;
using Tournaments.Core.Application;

namespace Tournaments.Api.Authentication;

public static class ActorExtensions
{
    public static Actor Actor(this ClaimsPrincipal principal) => new(
        principal.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException("Missing subject."), principal.IsInRole(Roles.Administrator));
}
