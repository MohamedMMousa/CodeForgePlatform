namespace CodeForge.Application.Modules.Common
{
    public record ModuleListDto(
        Guid Id,
        Guid CourseId,
        string Title,
        string? Description,
        int OrderIndex,
        int SessionCount,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
