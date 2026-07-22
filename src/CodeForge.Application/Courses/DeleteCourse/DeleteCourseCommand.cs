using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.DeleteCourse
{
    public record DeleteCourseCommand(Guid Id) : IRequest<CourseMutationResultDto>;
}
