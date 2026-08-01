using CodeForge.Application.Authentication.Common;
using MediatR;

namespace CodeForge.Application.Authentication.Login
{
    public record LoginCommand(string Email, string Password) : IRequest<AuthResult>;
}
