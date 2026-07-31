using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.GetAssignedCourses
{
    public record GetAssignedCoursesQuery(
        int Page = PaginationDefaults.Page,
        int PageSize = PaginationDefaults.PageSize) : IRequest<PagedResult<CourseListDto>>;
}
