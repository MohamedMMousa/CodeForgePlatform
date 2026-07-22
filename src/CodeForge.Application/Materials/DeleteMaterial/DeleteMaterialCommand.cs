using CodeForge.Application.Materials.Common;
using MediatR;

namespace CodeForge.Application.Materials.DeleteMaterial
{
    public record DeleteMaterialCommand(Guid Id) : IRequest<MaterialDto>;
}
