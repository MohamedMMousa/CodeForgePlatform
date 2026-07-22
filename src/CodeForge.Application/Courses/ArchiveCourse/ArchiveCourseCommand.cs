using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.ArchiveCourse
{
    public record ArchiveCourseCommand(Guid Id) : IRequest<CourseMutationResultDto>;
}
