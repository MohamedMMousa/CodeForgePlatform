using CodeForge.Application.Cohorts.Common;
using CodeForge.Application.Common.Constants;

namespace CodeForge.Application.Courses.Common
{
    /// <summary>
    /// Picks the cohort a catalog card should surface for a course, out of that course's
    /// <see cref="CohortStatuses.Open"/> cohorts. Pure and DbContext-free by design (see
    /// tests/CodeForge.UnitTests's calculator convention — e.g. AttendanceRateCalculator)
    /// so the rule is unit-testable without a database.
    ///
    /// Mirrors <see cref="Cohorts.Common.CohortMapping.ToDto"/>'s
    /// <c>isAcceptingEnrollment</c> predicate exactly
    /// (<c>status == Open &amp;&amp; now &lt;= cutoff &amp;&amp; enrolled &lt; capacity</c>)
    /// and <see cref="CohortAvailability.FindOpenCohortAsync"/>'s selection — a card must
    /// never promise a seat the enrollment path would then reject. Callers are expected
    /// to have already filtered candidates to <see cref="CohortStatuses.Open"/>.
    /// </summary>
    public static class NextCohortSelector
    {
        public record Candidate(
            Guid CohortId,
            string Name,
            DateTime StartDate,
            DateTime EnrollmentCutoffDate,
            int Capacity,
            int EnrolledCount);

        /// <summary>
        /// Returns the earliest-starting bookable candidate (tie-broken by
        /// <see cref="Candidate.CohortId"/> for determinism, matching the tiebreaker
        /// discipline API_CONVENTIONS.md §6 requires elsewhere), or <c>null</c> when
        /// nothing is bookable — which the catalog reads as "awaiting next batch", not a
        /// third status value. A record with a status but no honest date/seats is the
        /// shape that invites a fake "open" or a null-ref downstream, so this returns
        /// nothing instead.
        /// </summary>
        public static NextCohortSummaryDto? Select(IEnumerable<Candidate> openCohorts, DateTime now)
        {
            var bookable = openCohorts
                .Where(c => now <= c.EnrollmentCutoffDate && c.EnrolledCount < c.Capacity)
                .OrderBy(c => c.StartDate).ThenBy(c => c.CohortId)
                .FirstOrDefault();

            if (bookable is null)
            {
                return null;
            }

            var seatsLeft = Math.Max(0, bookable.Capacity - bookable.EnrolledCount);
            var status = seatsLeft <= CohortAvailabilityDefaults.AlmostFullSeatsThreshold
                ? NextCohortStatuses.AlmostFull
                : NextCohortStatuses.Open;

            return new NextCohortSummaryDto(
                bookable.CohortId,
                bookable.Name,
                bookable.StartDate,
                seatsLeft,
                status);
        }
    }
}
