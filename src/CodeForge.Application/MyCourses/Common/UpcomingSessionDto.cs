namespace CodeForge.Application.MyCourses.Common
{
    public record UpcomingSessionDto(
        Guid SessionId,
        Guid CourseId,
        string CourseTitle,
        string ModuleTitle,
        string Type,
        string Title,
        DateTime ScheduledAt,
        string? JoinLink,
        string? Location);
}
