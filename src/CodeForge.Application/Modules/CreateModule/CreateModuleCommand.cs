using CodeForge.Application.Modules.Common;
using MediatR;

namespace CodeForge.Application.Modules.CreateModule
{
    public record CreateModuleCommand(Guid CourseId, string Title, string? Description) : IRequest<ModuleResponseDto>;
}
