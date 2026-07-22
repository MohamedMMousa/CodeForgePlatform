using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.GetAssignedCourses
{
    public record GetAssignedCoursesQuery : IRequest<IReadOnlyList<CourseListDto>>;
}
