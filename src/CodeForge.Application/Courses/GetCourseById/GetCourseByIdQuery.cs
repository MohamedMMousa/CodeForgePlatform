using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.GetCourseById
{
    public record GetCourseByIdQuery(Guid Id) : IRequest<CourseDetailDto>;
}
