using CodeForge.Application.Users.Common;
using MediatR;

namespace CodeForge.Application.Users.ReactivateUser
{
    public record ReactivateUserCommand(Guid UserId) : IRequest<UserDto>;
}
