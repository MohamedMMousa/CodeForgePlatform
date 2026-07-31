using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.GetPublishedCourses
{
    public record GetPublishedCoursesQuery(
        string? Category,
        string? Search,
        int Page = PaginationDefaults.Page,
        int PageSize = PaginationDefaults.PageSize) : IRequest<PagedResult<CourseListDto>>;
}
