using CodeForge.Application.Authentication.Common;
using MediatR;

namespace CodeForge.Application.Authentication.ResetPassword
{
    public record ResetPasswordCommand(
        string Email,
        string Token,
        string NewPassword) : IRequest<AuthMessageResponse>;
}
