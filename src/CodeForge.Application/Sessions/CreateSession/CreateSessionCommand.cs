using CodeForge.Application.Sessions.Common;
using MediatR;

namespace CodeForge.Application.Sessions.CreateSession
{
    public record CreateSessionCommand(
        Guid ModuleId,
        string Type,
        string Title,
        string? Description,
        DateTime? ScheduledAt,
        int? DurationMinutes,
        string? JoinLink,
        string? Location,
        string? VideoUrl,
        Guid? InstructorId) : IRequest<SessionResponseDto>;
}
