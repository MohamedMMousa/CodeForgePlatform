using CodeForge.Application.Certificates.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Certificates.GetCertificateById
{
    public class GetCertificateByIdQueryHandler : IRequestHandler<GetCertificateByIdQuery, CertificateDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetCertificateByIdQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CertificateDto> Handle(GetCertificateByIdQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var certificate = await _context.Certificates
                .AsNoTracking()
                .Include(c => c.Student)
                .Include(c => c.Course).ThenInclude(course => course.Instructors)
                .Include(c => c.Cohort)
                .Include(c => c.IssuedBy)
                .FirstOrDefaultAsync(c => c.Id == request.CertificateId, cancellationToken);
            if (certificate is null)
            {
                throw new KeyNotFoundException("Certificate was not found.");
            }

            EnsureCanView(certificate, currentUserId);

            return CertificateMapping.ToDto(certificate);
        }

        private void EnsureCanView(Domain.Entities.Certificate certificate, Guid currentUserId)
        {
            if (_currentUserService.Role == Roles.Admin)
            {
                return;
            }

            if (_currentUserService.Role == Roles.Student && certificate.StudentId == currentUserId)
            {
                return;
            }

            if (_currentUserService.Role == Roles.Instructor
                && certificate.Course.Instructors.Any(i => i.InstructorId == currentUserId))
            {
                return;
            }

            throw new UnauthorizedAccessException("User does not have permission to view this certificate.");
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
