using CodeForge.Domain.Entities;

namespace CodeForge.Application.Materials.Common
{
    public static class MaterialMapping
    {
        public static MaterialDto ToDto(Material material)
        {
            return new MaterialDto(
                material.Id,
                material.ModuleId,
                material.SessionId,
                material.Type,
                material.Title,
                material.OrderIndex,
                material.Body,
                material.FileUrl,
                material.FileType,
                material.FileSizeKb,
                material.LinkUrl,
                material.CreatedAt,
                material.UpdatedAt);
        }
    }
}
