using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Common
{
    /// <summary>
    /// Cohort-availability is always computed, never stored — see docs/DATABASE.md §4.
    /// Shared by the Cohorts module and the EnrollmentRequests submission/approval
    /// paths so they can never disagree on what "open" or "full" means.
    /// </summary>
    public static class CohortAvailability
    {
        public static async Task<Cohort?> FindOpenCohortAsync(
            ICodeForgeDbContext context,
            Guid courseId,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var candidates = await context.Cohorts
                .Where(c => c.CourseId == courseId
                    && c.Status == CohortStatuses.Open
                    && c.EnrollmentCutoffDate >= now)
                .OrderBy(c => c.StartDate)
                .ToListAsync(cancellationToken);

            foreach (var cohort in candidates)
            {
                var enrolledCount = await context.Enrollments.CountAsync(
                    e => e.CohortId == cohort.Id && e.Status == EnrollmentStatuses.Active,
                    cancellationToken);

                if (enrolledCount < cohort.Capacity)
                {
                    return cohort;
                }
            }

            return null;
        }

        public static async Task<int> GetActiveEnrollmentCountAsync(
            ICodeForgeDbContext context,
            Guid cohortId,
            CancellationToken cancellationToken)
        {
            return await context.Enrollments.CountAsync(
                e => e.CohortId == cohortId && e.Status == EnrollmentStatuses.Active,
                cancellationToken);
        }
    }
}
