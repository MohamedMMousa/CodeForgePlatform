using CodeForge.Application.Certificates.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Certificates.GetMyCertificates
{
    public class GetMyCertificatesQueryHandler : IRequestHandler<GetMyCertificatesQuery, PagedResult<CertificateDto>>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetMyCertificatesQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<PagedResult<CertificateDto>> Handle(GetMyCertificatesQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var query = _context.Certificates
                .AsNoTracking()
                .Include(c => c.Student)
                .Include(c => c.Course)
                .Include(c => c.Cohort)
                .Include(c => c.IssuedBy)
                .Where(c => c.StudentId == currentUserId);

            var totalCount = await query.CountAsync(cancellationToken);

            var certificates = await query
                .OrderByDescending(c => c.IssuedAt).ThenBy(c => c.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = certificates.Select(CertificateMapping.ToDto).ToList();

            return new PagedResult<CertificateDto>(items, request.Page, request.PageSize, totalCount);
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
