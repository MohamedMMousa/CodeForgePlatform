using MediatR;

namespace CodeForge.Application.Authentication.Logout
{
    /// <summary>
    /// Revokes the refresh token identified by its plaintext value, if it resolves to a
    /// user. Deliberately does not throw when it doesn't — logout must be idempotent and
    /// must never fail, since the caller may already be signed out or presenting a stale
    /// token. See LogoutCommandHandler.
    /// </summary>
    public record LogoutCommand(string? RefreshToken) : IRequest;
}
