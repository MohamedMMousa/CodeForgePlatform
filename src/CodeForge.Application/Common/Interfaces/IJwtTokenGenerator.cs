using CodeForge.Domain.Entities;

namespace CodeForge.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
        string GenerateRefreshToken();

        /// <summary>
        /// Produces a deterministic hash of a refresh/reset token for storage at rest.
        /// The plaintext token is returned to the client; only the hash is persisted,
        /// so a database leak cannot be replayed against the token endpoints.
        /// </summary>
        string HashToken(string token);
    }
}
