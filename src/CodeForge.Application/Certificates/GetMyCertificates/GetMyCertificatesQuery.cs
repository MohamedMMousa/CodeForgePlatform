using CodeForge.Application.Certificates.Common;
using MediatR;

namespace CodeForge.Application.Certificates.GetMyCertificates
{
    public record GetMyCertificatesQuery() : IRequest<IReadOnlyList<CertificateDto>>;
}
