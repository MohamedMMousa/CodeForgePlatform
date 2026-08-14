namespace CodeForge.Application.Cohorts.Common
{
    /// <summary>
    /// Minimal, computed summary of the bookable cohort a catalog card would surface —
    /// see <see cref="Courses.Common.NextCohortSelector"/>. Deliberately narrower than
    /// <see cref="CohortListDto"/>: a list card needs a status, a start date, and seats
    /// remaining, nothing else (no instructors, no capacity/enrolled counts, no end date
    /// or cutoff).
    /// </summary>
    public record NextCohortSummaryDto(
        Guid CohortId,
        string Name,
        DateTime StartDate,
        int SeatsLeft,
        string Status);
}
