using CodeForge.Application.Sessions.Common;

namespace CodeForge.Application.MyCourses.Common
{
    public record MyCourseAssessmentDto(
        Guid Id,
        string Type,
        string Title,
        int? TimeLimitMinutes,
        int? PassScore,
        int? MaxAttempts,
        bool IsPractice);

    public record MyCourseAssignmentDto(
        Guid Id,
        string Title,
        DateTime? DueAt,
        int? MaxAttempts,
        bool IsPractice);

    public record MyCourseModuleDto(
        Guid Id,
        string Title,
        string? Description,
        int OrderIndex,
        IReadOnlyList<SessionDto> Sessions,
        IReadOnlyList<MyCourseAssessmentDto> Assessments,
        IReadOnlyList<MyCourseAssignmentDto> Assignments);
}
