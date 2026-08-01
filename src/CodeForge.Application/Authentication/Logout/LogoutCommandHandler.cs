using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Authentication.Logout
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public LogoutCommandHandler(ICodeForgeDbContext context, IJwtTokenGenerator jwtTokenGenerator)
        {
            _context = context;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return;
            }

            var hashedToken = _jwtTokenGenerator.HashToken(request.RefreshToken);
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.RefreshToken == hashedToken, cancellationToken);

            if (user is null)
            {
                return;
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
