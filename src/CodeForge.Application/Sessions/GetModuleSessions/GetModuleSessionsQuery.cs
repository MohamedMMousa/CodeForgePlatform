using CodeForge.Application.Sessions.Common;
using MediatR;

namespace CodeForge.Application.Sessions.GetModuleSessions
{
    public record GetModuleSessionsQuery(Guid ModuleId) : IRequest<IReadOnlyList<SessionDto>>;
}
