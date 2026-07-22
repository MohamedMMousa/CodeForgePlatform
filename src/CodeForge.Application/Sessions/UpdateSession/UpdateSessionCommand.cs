using CodeForge.Application.Sessions.Common;
using MediatR;

namespace CodeForge.Application.Sessions.UpdateSession
{
    public record UpdateSessionCommand(
        Guid Id,
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
