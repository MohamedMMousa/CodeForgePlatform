namespace CodeForge.Application.Leads.Common
{
    public record LeadDto(
        Guid Id,
        string Name,
        string Email,
        string? Phone,
        string? Message,
        Guid? CourseId,
        string? CourseTitle,
        bool IsContacted,
        DateTime CreatedAt);
}
