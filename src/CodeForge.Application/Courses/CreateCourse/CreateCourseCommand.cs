using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.CreateCourse
{
    public record CreateCourseCommand(
        string Title,
        string Slug,
        string? Description,
        string? ThumbnailUrl,
        string? Category,
        decimal Price,
        string Currency) : IRequest<CourseDetailDto>;
}
