using CodeForge.Application.Certificates.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Certificates.RevokeCertificate
{
    public class RevokeCertificateCommandHandler : IRequestHandler<RevokeCertificateCommand, CertificateDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public RevokeCertificateCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CertificateDto> Handle(RevokeCertificateCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();

            var certificate = await _context.Certificates
                .Include(c => c.Student)
                .Include(c => c.Course)
                .Include(c => c.Cohort)
                .Include(c => c.IssuedBy)
                .FirstOrDefaultAsync(c => c.Id == request.CertificateId, cancellationToken);
            if (certificate is null)
            {
                throw new KeyNotFoundException("Certificate was not found.");
            }

            if (certificate.IsRevoked)
            {
                throw new InvalidOperationException("Certificate is already revoked.");
            }

            certificate.IsRevoked = true;
            certificate.RevokedAt = DateTime.UtcNow;
            certificate.RevokedById = adminId;
            certificate.RevocationReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "certificate.revoked", nameof(Certificate), certificate.Id,
                new { certificate.SerialNumber, certificate.StudentId, certificate.CourseId }));

            await _context.SaveChangesAsync(cancellationToken);

            return CertificateMapping.ToDto(certificate);
        }

        private Guid GetCurrentUserId()
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated admin could not be resolved.");
            }

            return userId;
        }
    }
}
