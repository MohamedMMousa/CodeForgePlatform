using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Leads.Common;
using MediatR;

namespace CodeForge.Application.Leads.GetLeads
{
    public record GetLeadsQuery(
        bool? IsContacted,
        Guid? CourseId,
        int Page = PaginationDefaults.Page,
        int PageSize = PaginationDefaults.PageSize) : IRequest<PagedResult<LeadDto>>;
}
