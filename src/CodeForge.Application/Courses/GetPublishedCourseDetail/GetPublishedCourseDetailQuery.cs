using CodeForge.Application.Courses.Common;
using MediatR;

namespace CodeForge.Application.Courses.GetPublishedCourseDetail
{
    public record GetPublishedCourseDetailQuery(string Slug) : IRequest<PublicCourseDetailDto>;
}
