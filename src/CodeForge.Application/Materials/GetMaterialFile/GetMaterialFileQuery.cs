using MediatR;

namespace CodeForge.Application.Materials.GetMaterialFile
{
    public record GetMaterialFileQuery(Guid MaterialId) : IRequest<MaterialFileResult>;

    public record MaterialFileResult(Stream Stream, string ContentType, string FileName);
}
