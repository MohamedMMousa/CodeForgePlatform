using CodeForge.Application.Authentication.Common;
using MediatR;

namespace CodeForge.Application.Authentication.ChangePassword
{
    public record ChangePasswordCommand(
        string CurrentPassword,
        string NewPassword) : IRequest<AuthMessageResponse>;
}
