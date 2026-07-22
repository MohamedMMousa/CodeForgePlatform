using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.AssignInstructorToCourse
{
    public record AssignInstructorToCourseCommand(
        Guid CourseId,
        Guid InstructorId) : IRequest<CourseInstructorDto>;
}
