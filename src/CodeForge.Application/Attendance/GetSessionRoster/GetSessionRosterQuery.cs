using MediatR;

namespace CodeForge.Application.Attendance.GetSessionRoster
{
    public record RosterEntryDto(Guid StudentId, string StudentName, string? Status, string? Notes);

    public record SessionRosterDto(Guid SessionId, string SessionTitle, List<RosterEntryDto> Students);

    public record GetSessionRosterQuery(Guid SessionId) : IRequest<SessionRosterDto>;
}
