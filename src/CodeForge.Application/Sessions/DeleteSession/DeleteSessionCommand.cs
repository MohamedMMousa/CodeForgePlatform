using CodeForge.Application.Sessions.Common;
using MediatR;

namespace CodeForge.Application.Sessions.DeleteSession
{
    public record DeleteSessionCommand(Guid Id) : IRequest<SessionResponseDto>;
}
