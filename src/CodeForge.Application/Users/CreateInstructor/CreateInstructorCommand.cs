using CodeForge.Application.Users.Common;
using MediatR;

namespace CodeForge.Application.Users.CreateInstructor
{
    public record CreateInstructorCommand(
        string FullName,
        string Email,
        string? Phone) : IRequest<UserDto>;
}
