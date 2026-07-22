using CodeForge.Application.Authentication.Common;
using MediatR;

namespace CodeForge.Application.Authentication.ForgotPassword
{
    public record ForgotPasswordCommand(string Email) : IRequest<AuthMessageResponse>;
}
