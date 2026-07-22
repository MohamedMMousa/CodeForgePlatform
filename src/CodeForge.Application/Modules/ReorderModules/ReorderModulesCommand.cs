using MediatR;

namespace CodeForge.Application.Modules.ReorderModules
{
    public record ModuleOrderDto(Guid ModuleId, int OrderIndex);

    public record ReorderModulesCommand(Guid CourseId, List<ModuleOrderDto> ModuleOrders) : IRequest;
}
