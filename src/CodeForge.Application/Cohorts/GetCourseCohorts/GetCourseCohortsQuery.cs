using CodeForge.Application.Cohorts.Common;
using MediatR;

namespace CodeForge.Application.Cohorts.GetCourseCohorts
{
    public record GetCourseCohortsQuery(Guid CourseId) : IRequest<IReadOnlyList<CohortListDto>>;
}
