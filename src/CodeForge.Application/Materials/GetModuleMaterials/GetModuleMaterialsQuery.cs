using CodeForge.Application.Materials.Common;
using MediatR;

namespace CodeForge.Application.Materials.GetModuleMaterials
{
    public record GetModuleMaterialsQuery(Guid ModuleId) : IRequest<IReadOnlyList<MaterialDto>>;
}
