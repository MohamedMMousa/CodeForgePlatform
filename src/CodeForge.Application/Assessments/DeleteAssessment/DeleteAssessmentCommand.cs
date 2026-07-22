using CodeForge.Application.Assessments.Common;
using MediatR;

namespace CodeForge.Application.Assessments.DeleteAssessment
{
    public record DeleteAssessmentCommand(Guid Id) : IRequest<AssessmentResponseDto>;
}
