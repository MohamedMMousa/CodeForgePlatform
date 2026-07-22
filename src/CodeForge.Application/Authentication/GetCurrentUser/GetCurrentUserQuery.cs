using CodeForge.Application.Authentication.Common;
using MediatR;

namespace CodeForge.Application.Authentication.GetCurrentUser
{
    public record GetCurrentUserQuery : IRequest<CurrentUserResponse>;
}
