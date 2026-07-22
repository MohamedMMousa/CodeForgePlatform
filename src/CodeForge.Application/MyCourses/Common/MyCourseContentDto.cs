namespace CodeForge.Application.MyCourses.Common
{
    public record MyCourseContentDto(
        Guid CourseId,
        string CourseTitle,
        IReadOnlyList<MyCourseModuleDto> Modules);
}
