using CodeForge.Application.Authentication.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeForge.Application.Authentication.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
    {
        // A lost compare-and-swap means another request won the same rotation race;
        // one retry re-reads the fresh state and resolves to ReturnCurrent/Reuse/Invalid
        // against it. A second loss would mean a third concurrent writer, which the
        // grace window doesn't attempt to chase indefinitely — fail safe instead.
        private const int MaxAttempts = 2;

        private readonly ICodeForgeDbContext _context;
        private readonly IRefreshTokenRotationStore _rotationStore;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly JwtSettings _jwtSettings;

        public RefreshTokenCommandHandler(
            ICodeForgeDbContext context,
            IRefreshTokenRotationStore rotationStore,
            IJwtTokenGenerator jwtTokenGenerator,
            IOptions<JwtSettings> jwtOptions)
        {
            _context = context;
            _rotationStore = rotationStore;
            _jwtTokenGenerator = jwtTokenGenerator;
            _jwtSettings = jwtOptions.Value;
        }

        public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var presentedHash = _jwtTokenGenerator.HashToken(request.RefreshToken);

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                // AsNoTracking is load-bearing here, not cosmetic: without it, a retry
                // iteration's "fresh" query would return the same tracked instance from
                // attempt 1 out of EF's identity map — stale rotation state — rather
                // than re-materializing the row TryRotateAsync just changed underneath
                // it via a bulk update the change tracker never sees.
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.RefreshToken == presentedHash || x.PreviousRefreshToken == presentedHash,
                        cancellationToken);

                if (user is null || !user.IsActive)
                {
                    throw new UnauthorizedAccessException("Invalid or expired refresh token.");
                }

                var outcome = RefreshTokenRotationPolicy.Decide(
                    user.RefreshToken, user.PreviousRefreshToken, user.RefreshTokenRotatedAt, presentedHash, now);

                switch (outcome)
                {
                    case RefreshTokenRotationOutcome.ReturnCurrent:
                        if (user.RefreshTokenExpiryTime is null || user.RefreshTokenExpiryTime <= now ||
                            user.PendingRefreshToken is null)
                        {
                            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
                        }

                        return new AuthResponse(
                            user.Id, user.Email, user.FullName, user.Role,
                            _jwtTokenGenerator.GenerateToken(user),
                            user.PendingRefreshToken,
                            user.RefreshTokenExpiryTime.Value,
                            user.MustChangePassword);

                    case RefreshTokenRotationOutcome.Reuse:
                        // Presented an already-superseded token outside the grace
                        // window: a genuine replay. Kill the session rather than trust
                        // either generation of it. No CAS needed here — worst case of
                        // a race is nulling twice, which is idempotent.
                        await _rotationStore.RevokeAsync(user.Id, cancellationToken);
                        throw new UnauthorizedAccessException("Invalid or expired refresh token.");

                    case RefreshTokenRotationOutcome.Invalid:
                        throw new UnauthorizedAccessException("Invalid or expired refresh token.");

                    case RefreshTokenRotationOutcome.Rotate:
                        var newPlaintext = _jwtTokenGenerator.GenerateRefreshToken();
                        var newHash = _jwtTokenGenerator.HashToken(newPlaintext);
                        var newExpiry = now.AddDays(_jwtSettings.RefreshTokenExpiryDays);

                        // Atomic compare-and-swap: only succeeds if RefreshToken is
                        // still exactly the hash we just read. If another concurrent
                        // request won the race first, this returns false and the next
                        // loop iteration re-reads the fresh state (which the winner's
                        // write now makes this request's presented token match as
                        // "previous", resolving to ReturnCurrent).
                        var won = await _rotationStore.TryRotateAsync(
                            user.Id, user.RefreshToken!, newHash, newPlaintext, now, newExpiry, cancellationToken);

                        if (won)
                        {
                            return new AuthResponse(
                                user.Id, user.Email, user.FullName, user.Role,
                                _jwtTokenGenerator.GenerateToken(user),
                                newPlaintext,
                                newExpiry,
                                user.MustChangePassword);
                        }

                        break; // lost the race — loop and re-read
                }
            }

            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }
    }
}
