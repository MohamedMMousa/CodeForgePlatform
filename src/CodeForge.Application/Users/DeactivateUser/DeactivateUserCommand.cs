using CodeForge.Application.Users.Common;
using MediatR;

namespace CodeForge.Application.Users.DeactivateUser
{
    public record DeactivateUserCommand(Guid UserId) : IRequest<UserDto>;
}
