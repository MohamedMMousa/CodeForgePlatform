namespace CodeForge.Application.Courses.Common
{
    public record CourseInstructorDto(
        Guid Id,
        Guid InstructorId,
        string FullName,
        string Email,
        DateTime AssignedAt);
}
