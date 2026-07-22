using CodeForge.Application.Users.Common;
using MediatR;

namespace CodeForge.Application.Users.GetUsers
{
    public record GetUsersQuery(string? Role, bool? IsActive, string? Search) : IRequest<IReadOnlyList<UserDto>>;
}
