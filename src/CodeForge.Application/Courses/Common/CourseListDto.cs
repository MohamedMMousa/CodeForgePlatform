using CodeForge.Application.Cohorts.Common;

namespace CodeForge.Application.Courses.Common
{
    /// <param name="NextCohort">
    /// Computed next bookable cohort for this course (see
    /// <see cref="NextCohortSelector"/>) — never stored. Carries two distinct meanings
    /// depending on the caller: on the public catalog (<c>GET /catalog/courses</c>),
    /// <c>null</c> means "no bookable cohort right now" (the card should read "awaiting
    /// next batch"). On the admin (<c>GET /courses</c>) and instructor
    /// (<c>GET /instructor/courses</c>) list endpoints it is always <c>null</c> because
    /// those surfaces have their own cohort-management UI and this is not computed for
    /// them. Either way, consumers must null-check rather than assume a meaning.
    /// </param>
    public record CourseListDto(
        Guid Id,
        string Title,
        string Slug,
        string? Description,
        string? ThumbnailUrl,
        string? Category,
        decimal Price,
        string Currency,
        string Status,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        NextCohortSummaryDto? NextCohort = null);
}
