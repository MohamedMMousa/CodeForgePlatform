namespace CodeForge.Application.Assignments.Common
{
    public record AssignmentDto(
        Guid Id,
        Guid ModuleId,
        string Title,
        string Description,
        int OrderIndex,
        bool IsPractice,
        int? MaxAttempts,
        DateTime? DueAt,
        int? PassScore,
        int TestCaseCount,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public record AssignmentDetailDto(
        Guid Id,
        Guid ModuleId,
        string Title,
        string Description,
        int OrderIndex,
        bool IsPractice,
        int? MaxAttempts,
        DateTime? DueAt,
        int? PassScore,
        List<TestCaseDto> TestCases);
}
