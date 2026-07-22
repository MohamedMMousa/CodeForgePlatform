using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.GetCourses
{
    public record GetCoursesQuery(
        string? Status,
        string? Category,
        string? Search) : IRequest<IReadOnlyList<CourseListDto>>;
}
