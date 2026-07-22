using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.RemoveInstructorFromCourse
{
    public record RemoveInstructorFromCourseCommand(
        Guid CourseId,
        Guid InstructorId) : IRequest<CourseMutationResultDto>;
}
