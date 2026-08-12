using CodeForge.Application.MyCourses.Common;
using MediatR;

namespace CodeForge.Application.MyCourses.GetMyCourses
{
    public record GetMyCoursesQuery : IRequest<IReadOnlyList<MyCourseSummaryDto>>;
}
