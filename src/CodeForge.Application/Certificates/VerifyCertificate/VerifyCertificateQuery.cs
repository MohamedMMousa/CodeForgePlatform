using CodeForge.Application.Certificates.Common;
using MediatR;

namespace CodeForge.Application.Certificates.VerifyCertificate
{
    // Public, unauthenticated lookup by the opaque verification code printed on the
    // certificate. Returns a minimal, privacy-conscious payload.
    public record VerifyCertificateQuery(string Code) : IRequest<CertificateVerificationDto>;
}
