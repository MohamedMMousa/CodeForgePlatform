using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.GetCourses
{
    public record GetCoursesQuery(
        string? Status,
        string? Category,
        string? Search,
        int Page = PaginationDefaults.Page,
        int PageSize = PaginationDefaults.PageSize) : IRequest<PagedResult<CourseListDto>>;
}
