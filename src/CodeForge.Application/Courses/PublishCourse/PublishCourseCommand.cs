using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.PublishCourse
{
    public record PublishCourseCommand(Guid Id) : IRequest<CourseMutationResultDto>;
}
