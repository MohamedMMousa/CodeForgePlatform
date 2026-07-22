using CodeForge.Application.Modules.Common;
using MediatR;

namespace CodeForge.Application.Modules.DeleteModule
{
    public record DeleteModuleCommand(Guid Id) : IRequest<ModuleResponseDto>;
}
