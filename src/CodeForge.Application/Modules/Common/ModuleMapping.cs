using CodeForge.Domain.Entities;

namespace CodeForge.Application.Modules.Common
{
    public static class ModuleMapping
    {
        public static ModuleListDto ToDto(Module module)
        {
            return new ModuleListDto(
                module.Id,
                module.CourseId,
                module.Title,
                module.Description,
                module.OrderIndex,
                module.Sessions.Count,
                module.CreatedAt,
                module.UpdatedAt);
        }
    }
}
