using CodeForge.Application.MyCourses.Common;
using MediatR;

namespace CodeForge.Application.MyCourses.GetMyCourseContent
{
    public record GetMyCourseContentQuery(Guid CourseId) : IRequest<MyCourseContentDto>;
}
