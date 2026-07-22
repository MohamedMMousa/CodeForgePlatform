using CodeForge.Application.Common.Constants;
using CodeForge.Domain.Entities;

namespace CodeForge.Application.Cohorts.Common
{
    public static class CohortMapping
    {
        /// <summary>
        /// Availability is always computed from live data, never stored — see
        /// docs/DATABASE.md §4. Caller supplies the enrolled count since counting
        /// active enrollments requires a query the mapper itself doesn't have access to.
        /// </summary>
        public static CohortListDto ToDto(Cohort cohort, int enrolledCount, DateTime now)
        {
            var seatsLeft = Math.Max(0, cohort.Capacity - enrolledCount);
            var isAcceptingEnrollment =
                cohort.Status == CohortStatuses.Open
                && now <= cohort.EnrollmentCutoffDate
                && enrolledCount < cohort.Capacity;

            return new CohortListDto(
                cohort.Id,
                cohort.CourseId,
                cohort.Course.Title,
                cohort.Name,
                cohort.StartDate,
                cohort.EndDate,
                cohort.EnrollmentCutoffDate,
                cohort.Capacity,
                cohort.GracePeriodDays,
                cohort.Status,
                enrolledCount,
                seatsLeft,
                isAcceptingEnrollment,
                cohort.CreatedAt,
                cohort.UpdatedAt);
        }
    }
}
