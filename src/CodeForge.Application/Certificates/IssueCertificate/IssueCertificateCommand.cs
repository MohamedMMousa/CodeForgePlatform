using CodeForge.Application.Certificates.Common;
using MediatR;

namespace CodeForge.Application.Certificates.IssueCertificate
{
    // Tier is optional: when null the server-computed recommended tier is used. Admins may
    // explicitly override (e.g. issue a Participation certificate even to someone who met
    // the completion bar). Admin only (enforced at the controller).
    public record IssueCertificateCommand(Guid EnrollmentId, string? Tier) : IRequest<CertificateDto>;
}
