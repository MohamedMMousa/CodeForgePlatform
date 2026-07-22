using CodeForge.Application.Sessions.Common;

namespace CodeForge.Application.MyCourses.Common
{
    public record MyCourseModuleDto(
        Guid Id,
        string Title,
        string? Description,
        int OrderIndex,
        IReadOnlyList<SessionDto> Sessions);
}
