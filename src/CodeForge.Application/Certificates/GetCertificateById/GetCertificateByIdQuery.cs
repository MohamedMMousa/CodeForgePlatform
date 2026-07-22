using CodeForge.Application.Certificates.Common;
using MediatR;

namespace CodeForge.Application.Certificates.GetCertificateById
{
    public record GetCertificateByIdQuery(Guid CertificateId) : IRequest<CertificateDto>;
}
