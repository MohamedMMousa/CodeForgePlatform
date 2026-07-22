using CodeForge.Application.Assessments.Common;
using MediatR;

namespace CodeForge.Application.Assessments.GetModuleAssessments
{
    public record GetModuleAssessmentsQuery(Guid ModuleId) : IRequest<IReadOnlyList<AssessmentDto>>;
}
