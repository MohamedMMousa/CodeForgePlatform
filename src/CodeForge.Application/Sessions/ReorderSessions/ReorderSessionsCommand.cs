using MediatR;

namespace CodeForge.Application.Sessions.ReorderSessions
{
    public record SessionOrderDto(Guid SessionId, int OrderIndex);

    public record ReorderSessionsCommand(Guid ModuleId, List<SessionOrderDto> SessionOrders) : IRequest;
}
