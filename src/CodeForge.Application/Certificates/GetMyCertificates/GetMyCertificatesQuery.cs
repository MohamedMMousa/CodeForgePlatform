using CodeForge.Application.Certificates.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using MediatR;

namespace CodeForge.Application.Certificates.GetMyCertificates
{
    public record GetMyCertificatesQuery(
        int Page = PaginationDefaults.Page,
        int PageSize = PaginationDefaults.PageSize) : IRequest<PagedResult<CertificateDto>>;
}
