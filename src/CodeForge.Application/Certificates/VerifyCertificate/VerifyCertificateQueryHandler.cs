using CodeForge.Application.Certificates.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Certificates.VerifyCertificate
{
    public class VerifyCertificateQueryHandler : IRequestHandler<VerifyCertificateQuery, CertificateVerificationDto>
    {
        private readonly ICodeForgeDbContext _context;

        public VerifyCertificateQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<CertificateVerificationDto> Handle(VerifyCertificateQuery request, CancellationToken cancellationToken)
        {
            var code = request.Code?.Trim() ?? string.Empty;

            var certificate = await _context.Certificates
                .AsNoTracking()
                .Include(c => c.Student)
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.VerificationCode == code, cancellationToken);

            if (certificate is null)
            {
                return new CertificateVerificationDto(
                    Found: false, IsValid: false, StudentName: null, CourseTitle: null,
                    Tier: null, SerialNumber: null, IssuedAt: null, IsRevoked: false);
            }

            return new CertificateVerificationDto(
                Found: true,
                IsValid: !certificate.IsRevoked,
                StudentName: certificate.Student.FullName,
                CourseTitle: certificate.Course.Title,
                Tier: certificate.Tier,
                SerialNumber: certificate.SerialNumber,
                IssuedAt: certificate.IssuedAt,
                IsRevoked: certificate.IsRevoked);
        }
    }
}
