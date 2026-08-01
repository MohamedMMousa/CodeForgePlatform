namespace CodeForge.Application.Authentication.RefreshToken
{
    public enum RefreshTokenRotationOutcome
    {
        /// <summary>Presented token is the current one and outside the grace window
        /// of the last rotation: mint and persist a new refresh token.</summary>
        Rotate,

        /// <summary>Presented token matches what was JUST superseded, within the grace
        /// window: a concurrent latecomer, not a replay. Hand back the already-rotated
        /// current token rather than rotating again.</summary>
        ReturnCurrent,

        /// <summary>Presented token matches what was superseded, but the grace window
        /// has closed: a genuine replay of an old token. Kill the session.</summary>
        Reuse,

        /// <summary>Presented token matches neither the current nor the previous
        /// token.</summary>
        Invalid
    }

    /// <summary>
    /// Pure decision logic for refresh-token rotation, extracted so it's unit-testable
    /// without a DbContext. Exists to make concurrent refreshes (e.g. several
    /// same-origin requests firing at once when an access token expires — Next.js
    /// prefetch fan-out is the concrete trigger) converge on the same new refresh
    /// token instead of racing to invalidate each other. See ARCHITECTURE.md §3 and
    /// RefreshTokenCommandHandler, which orchestrates the compare-and-swap this
    /// decision feeds into.
    /// </summary>
    public static class RefreshTokenRotationPolicy
    {
        public static readonly TimeSpan GraceWindow = TimeSpan.FromSeconds(30);

        public static RefreshTokenRotationOutcome Decide(
            string? currentHash,
            string? previousHash,
            DateTime? rotatedAt,
            string presentedHash,
            DateTime now)
        {
            if (currentHash is not null && currentHash == presentedHash)
            {
                return RefreshTokenRotationOutcome.Rotate;
            }

            if (previousHash is not null && previousHash == presentedHash)
            {
                var withinGrace = rotatedAt is not null && now - rotatedAt.Value <= GraceWindow;
                return withinGrace
                    ? RefreshTokenRotationOutcome.ReturnCurrent
                    : RefreshTokenRotationOutcome.Reuse;
            }

            return RefreshTokenRotationOutcome.Invalid;
        }
    }
}
