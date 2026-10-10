using LevelUp.Application.Common.Interfaces;

namespace LevelUp.Api.Services;

/// <summary>
/// Resolves the current user from the HTTP context. Falls back to "system" until authentication is added.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public const string AnonymousUser = "system";

    public string UserName
    {
        get
        {
            var name = httpContextAccessor.HttpContext?.User.Identity?.Name;
            return string.IsNullOrWhiteSpace(name) ? AnonymousUser : name;
        }
    }
}
