using CodeForge.Application.Leads.Common;
using MediatR;

namespace CodeForge.Application.Leads.GetLeads
{
    public record GetLeadsQuery(bool? IsContacted, Guid? CourseId) : IRequest<IReadOnlyList<LeadDto>>;
}
