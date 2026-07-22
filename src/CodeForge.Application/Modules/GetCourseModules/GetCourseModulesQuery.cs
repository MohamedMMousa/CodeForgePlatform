using CodeForge.Application.Modules.Common;
using MediatR;

namespace CodeForge.Application.Modules.GetCourseModules
{
    public record GetCourseModulesQuery(Guid CourseId) : IRequest<IReadOnlyList<ModuleListDto>>;
}
