using CodeForge.Application.Cohorts.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using MediatR;

namespace CodeForge.Application.Cohorts.GetCourseCohorts
{
    public record GetCourseCohortsQuery(
        Guid CourseId,
        int Page = PaginationDefaults.Page,
        int PageSize = PaginationDefaults.PageSize) : IRequest<PagedResult<CohortListDto>>;
}
