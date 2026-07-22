namespace CodeForge.Application.Sessions.Common
{
    public record SessionDto(
        Guid Id,
        Guid ModuleId,
        string Type,
        string Title,
        string? Description,
        int OrderIndex,
        DateTime? ScheduledAt,
        int? DurationMinutes,
        string? JoinLink,
        string? Location,
        string? VideoUrl,
        Guid? InstructorId,
        string? InstructorName,
        int MaterialCount,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
