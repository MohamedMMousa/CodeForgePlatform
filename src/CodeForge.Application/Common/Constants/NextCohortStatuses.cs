namespace CodeForge.Application.Common.Constants
{
    /// <summary>
    /// Computed, not stored — see <see cref="CohortStatuses"/> and docs/DATABASE.md §4.
    /// Describes the bookable cohort a catalog card would surface, distinct from the
    /// admin-controlled lifecycle status of the underlying cohort row.
    /// </summary>
    public static class NextCohortStatuses
    {
        public const string Open = "open";
        public const string AlmostFull = "almost_full";
    }
}
