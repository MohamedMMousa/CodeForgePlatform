using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.UpdateCourse
{
    public record UpdateCourseCommand(
        Guid Id,
        string Title,
        string Slug,
        string? Description,
        string? ThumbnailUrl,
        string? Category,
        decimal Price,
        string Currency,
        decimal? CompletionAttendanceThreshold) : IRequest<CourseDetailDto>;
}
