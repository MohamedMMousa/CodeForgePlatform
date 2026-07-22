using CodeForge.Application.Assessments.Common;
using MediatR;

namespace CodeForge.Application.Assessments.GetAssessmentById
{
    public record GetAssessmentByIdQuery(Guid Id) : IRequest<AssessmentDetailDto>;
}
