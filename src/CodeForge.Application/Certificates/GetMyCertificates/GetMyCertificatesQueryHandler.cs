using CodeForge.Application.Certificates.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Certificates.GetMyCertificates
{
    public class GetMyCertificatesQueryHandler : IRequestHandler<GetMyCertificatesQuery, IReadOnlyList<CertificateDto>>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetMyCertificatesQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IReadOnlyList<CertificateDto>> Handle(GetMyCertificatesQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var certificates = await _context.Certificates
                .AsNoTracking()
                .Include(c => c.Student)
                .Include(c => c.Course)
                .Include(c => c.Cohort)
                .Include(c => c.IssuedBy)
                .Where(c => c.StudentId == currentUserId)
                .OrderByDescending(c => c.IssuedAt)
                .ToListAsync(cancellationToken);

            return certificates.Select(CertificateMapping.ToDto).ToList();
        }

        private Guid GetCurrentUserId()
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated user could not be resolved.");
            }

            return userId;
        }
    }
}
