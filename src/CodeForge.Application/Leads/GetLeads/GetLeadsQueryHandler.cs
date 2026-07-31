using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Leads.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Leads.GetLeads
{
    public class GetLeadsQueryHandler : IRequestHandler<GetLeadsQuery, PagedResult<LeadDto>>
    {
        private readonly ICodeForgeDbContext _context;

        public GetLeadsQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<LeadDto>> Handle(GetLeadsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Leads.AsNoTracking().Include(x => x.Course).AsQueryable();

            if (request.IsContacted.HasValue)
            {
                query = query.Where(x => x.IsContacted == request.IsContacted.Value);
            }

            if (request.CourseId.HasValue)
            {
                query = query.Where(x => x.CourseId == request.CourseId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var leads = await query
                .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = leads.Select(lead => new LeadDto(
                lead.Id, lead.Name, lead.Email, lead.Phone, lead.Message,
                lead.CourseId, lead.Course?.Title, lead.IsContacted, lead.CreatedAt))
                .ToList();

            return new PagedResult<LeadDto>(items, request.Page, request.PageSize, totalCount);
        }
    }
}
