namespace CodeForge.Application.Common.Interfaces
{
    /// <summary>
    /// Atomic compare-and-swap for refresh-token rotation. Kept behind an interface
    /// (implemented in Infrastructure) because the atomicity guarantee — an UPDATE
    /// that only takes effect if the row's current hash still matches what the
    /// caller read — needs a relational bulk-update API (EF's ExecuteUpdateAsync)
    /// that lives in the Relational package, which Application doesn't reference.
    /// See RefreshTokenCommandHandler for how the compare-and-swap result feeds the
    /// rotation-grace-window retry loop.
    /// </summary>
    public interface IRefreshTokenRotationStore
    {
        /// <summary>
        /// Rotates the refresh token for the given user, but only if its current hash
        /// still equals <paramref name="expectedCurrentHash"/> at write time. Returns
        /// false if another request already rotated it first — the caller should
        /// re-read the user and re-evaluate against the fresh state.
        /// </summary>
        Task<bool> TryRotateAsync(
            Guid userId,
            string expectedCurrentHash,
            string newHash,
            string newPlaintext,
            DateTime rotatedAt,
            DateTime newExpiry,
            CancellationToken cancellationToken);

        /// <summary>Unconditionally clears all refresh-rotation state for the user —
        /// used on reuse detection. A bulk update rather than a tracked-entity mutation
        /// so it never returns a stale change-tracker snapshot to a caller that reads
        /// again afterward (see RefreshTokenCommandHandler's retry loop).</summary>
        Task RevokeAsync(Guid userId, CancellationToken cancellationToken);
    }
}
