using CodeForge.Application.Authentication.Common;
using MediatR;

namespace CodeForge.Application.Authentication.RefreshToken
{
    public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResult>;
}
