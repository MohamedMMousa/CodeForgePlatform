using MediatR;

namespace CodeForge.Application.Assessments.StartAttempt
{
    public record StartAttemptResponseDto(Guid AttemptId, DateTime StartedAt);

    public record StartAttemptCommand(Guid AssessmentId) : IRequest<StartAttemptResponseDto>;
}
