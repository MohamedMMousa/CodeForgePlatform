using CodeForge.Application.Common.Constants;
using CodeForge.Domain.Entities;

namespace CodeForge.Application.Materials.Common
{
    public static class MaterialMapping
    {
        public static MaterialDto ToDto(Material material)
        {
            // FileUrl on the entity is now an opaque private storage key (see
            // IFileStorageService), never handed to clients directly — the client
            // downloads through the authenticated /materials/{id}/file endpoint.
            var downloadUrl = material.Type == MaterialTypes.File && material.FileUrl is not null
                ? $"/materials/{material.Id}/file"
                : null;

            return new MaterialDto(
                material.Id,
                material.ModuleId,
                material.SessionId,
                material.Type,
                material.Title,
                material.OrderIndex,
                material.Body,
                downloadUrl,
                material.FileType,
                material.FileSizeKb,
                material.LinkUrl,
                material.CreatedAt,
                material.UpdatedAt);
        }
    }
}
