using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.GetPublishedCourses
{
    public record GetPublishedCoursesQuery(
        string? Category,
        string? Search) : IRequest<IReadOnlyList<CourseListDto>>;
}
