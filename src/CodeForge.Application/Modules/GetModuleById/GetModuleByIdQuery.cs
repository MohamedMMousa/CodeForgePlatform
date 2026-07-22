using CodeForge.Application.Modules.Common;
using MediatR;

namespace CodeForge.Application.Modules.GetModuleById
{
    public record GetModuleByIdQuery(Guid Id) : IRequest<ModuleListDto>;
}
