namespace CodeForge.Application.Common.Constants
{
    /// <summary>
    /// Tuning values for how computed cohort availability reads in the UI. See
    /// <see cref="CohortAvailability"/> and <see cref="NextCohortStatuses"/>.
    /// </summary>
    public static class CohortAvailabilityDefaults
    {
        /// <summary>
        /// Seats at or below which a bookable cohort reads as "almost full" rather than
        /// plain "open". Matches the threshold already shown on the course-detail page
        /// (frontend/app/[locale]/catalog/courses/[slug]/page.tsx) so the catalog card
        /// and the page it links to never disagree.
        /// </summary>
        public const int AlmostFullSeatsThreshold = 3;
    }
}
