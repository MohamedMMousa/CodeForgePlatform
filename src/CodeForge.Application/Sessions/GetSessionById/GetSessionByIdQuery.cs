using CodeForge.Application.Sessions.Common;
using MediatR;

namespace CodeForge.Application.Sessions.GetSessionById
{
    public record GetSessionByIdQuery(Guid Id) : IRequest<SessionDto>;
}
