using CodeForge.Application.Materials.Common;
using MediatR;

namespace CodeForge.Application.Materials.CreateMaterial
{
    public record CreateMaterialCommand(
        Guid? ModuleId,
        Guid? SessionId,
        string Type,
        string Title,
        string? Body,
        string? LinkUrl,
        string? FileType,
        Stream? FileStream,
        string? FileName,
        string? ContentType) : IRequest<MaterialDto>;
}
