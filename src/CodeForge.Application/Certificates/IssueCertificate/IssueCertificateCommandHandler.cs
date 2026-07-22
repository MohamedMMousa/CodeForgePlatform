using CodeForge.Application.Certificates.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Certificates.IssueCertificate
{
    public class IssueCertificateCommandHandler : IRequestHandler<IssueCertificateCommand, CertificateDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public IssueCertificateCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CertificateDto> Handle(IssueCertificateCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();

            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == request.EnrollmentId, cancellationToken);
            if (enrollment is null)
            {
                throw new KeyNotFoundException("Enrollment was not found.");
            }

            if (enrollment.Status is EnrollmentStatuses.Cancelled or EnrollmentStatuses.Refunded)
            {
                throw new InvalidOperationException("A certificate cannot be issued for a cancelled or refunded enrollment.");
            }

            var alreadyIssued = await _context.Certificates
                .AnyAsync(c => c.EnrollmentId == request.EnrollmentId, cancellationToken);
            if (alreadyIssued)
            {
                throw new InvalidOperationException("A certificate has already been issued for this enrollment.");
            }

            var evaluation = await CourseEligibilityEvaluator.EvaluateAsync(_context, enrollment.CourseId, cancellationToken);
            var computed = evaluation?.Enrollments.FirstOrDefault(e => e.Enrollment.Id == request.EnrollmentId);
            if (computed is null)
            {
                // Should not happen for a certifiable enrollment, but guard defensively.
                throw new InvalidOperationException("Eligibility for this enrollment could not be computed.");
            }

            var tier = request.Tier ?? computed.Result.Tier;

            var certificate = new Certificate
            {
                EnrollmentId = enrollment.Id,
                StudentId = enrollment.StudentId,
                CourseId = enrollment.CourseId,
                CohortId = enrollment.CohortId,
                Tier = tier,
                SerialNumber = CertificateCodeGenerator.NewSerialNumber(DateTime.UtcNow.Year),
                VerificationCode = CertificateCodeGenerator.NewVerificationCode(),
                AttendanceRate = computed.AttendanceRate,
                AssessmentsPassed = computed.Result.AssessmentsPassed,
                IssuedById = adminId,
                IssuedAt = DateTime.UtcNow,
            };

            _context.Certificates.Add(certificate);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "certificate.issued", nameof(Certificate), certificate.Id,
                new { certificate.EnrollmentId, certificate.StudentId, certificate.CourseId, certificate.Tier, certificate.SerialNumber }));

            await _context.SaveChangesAsync(cancellationToken);

            return await LoadDtoAsync(certificate.Id, cancellationToken);
        }

        private async Task<CertificateDto> LoadDtoAsync(Guid id, CancellationToken cancellationToken)
        {
            var saved = await _context.Certificates
                .AsNoTracking()
                .Include(c => c.Student)
                .Include(c => c.Course)
                .Include(c => c.Cohort)
                .Include(c => c.IssuedBy)
                .FirstAsync(c => c.Id == id, cancellationToken);
            return CertificateMapping.ToDto(saved);
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
