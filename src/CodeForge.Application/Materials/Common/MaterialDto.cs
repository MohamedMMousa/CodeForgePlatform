namespace CodeForge.Application.Materials.Common
{
    public record MaterialDto(
        Guid Id,
        Guid? ModuleId,
        Guid? SessionId,
        string Type,
        string Title,
        int OrderIndex,
        string? Body,
        string? FileUrl,
        string? FileType,
        int? FileSizeKb,
        string? LinkUrl,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
