using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.GetCourseInstructors
{
    public record GetCourseInstructorsQuery(Guid CourseId) : IRequest<IReadOnlyList<CourseInstructorDto>>;
}
