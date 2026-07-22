using CodeForge.Application.Modules.Common;
using MediatR;

namespace CodeForge.Application.Modules.UpdateModule
{
    public record UpdateModuleCommand(Guid Id, string Title, string? Description) : IRequest<ModuleResponseDto>;
}
