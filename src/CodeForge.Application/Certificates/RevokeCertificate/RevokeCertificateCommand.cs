using CodeForge.Application.Certificates.Common;
using MediatR;

namespace CodeForge.Application.Certificates.RevokeCertificate
{
    // Admin only (enforced at the controller). Keeps the record for audit — a revoked
    // certificate still verifies, but as invalid.
    public record RevokeCertificateCommand(Guid CertificateId, string? Reason) : IRequest<CertificateDto>;
}
