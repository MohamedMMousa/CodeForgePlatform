using CodeForge.Application.Common.Interfaces;
using CodeForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Infrastructure.Authentication
{
    public class RefreshTokenRotationStore : IRefreshTokenRotationStore
    {
        private readonly CodeForgeDbContext _context;

        public RefreshTokenRotationStore(CodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<bool> TryRotateAsync(
            Guid userId,
            string expectedCurrentHash,
            string newHash,
            string newPlaintext,
            DateTime rotatedAt,
            DateTime newExpiry,
            CancellationToken cancellationToken)
        {
            var rowsAffected = await _context.Users
                .Where(u => u.Id == userId && u.RefreshToken == expectedCurrentHash)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.PreviousRefreshToken, expectedCurrentHash)
                    .SetProperty(u => u.RefreshToken, newHash)
                    .SetProperty(u => u.PendingRefreshToken, newPlaintext)
                    .SetProperty(u => u.RefreshTokenRotatedAt, rotatedAt)
                    .SetProperty(u => u.RefreshTokenExpiryTime, newExpiry),
                    cancellationToken);

            return rowsAffected == 1;
        }

        public Task RevokeAsync(Guid userId, CancellationToken cancellationToken) =>
            _context.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.RefreshToken, u => (string?)null)
                    .SetProperty(u => u.PreviousRefreshToken, u => (string?)null)
                    .SetProperty(u => u.PendingRefreshToken, u => (string?)null)
                    .SetProperty(u => u.RefreshTokenRotatedAt, u => (DateTime?)null)
                    .SetProperty(u => u.RefreshTokenExpiryTime, u => (DateTime?)null),
                    cancellationToken);
    }
}
