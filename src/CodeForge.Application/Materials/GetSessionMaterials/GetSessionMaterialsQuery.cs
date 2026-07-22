using CodeForge.Application.Materials.Common;
using MediatR;

namespace CodeForge.Application.Materials.GetSessionMaterials
{
    public record GetSessionMaterialsQuery(Guid SessionId) : IRequest<IReadOnlyList<MaterialDto>>;
}
