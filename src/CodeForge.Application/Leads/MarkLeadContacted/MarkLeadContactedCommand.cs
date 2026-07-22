using CodeForge.Application.Leads.Common;
using MediatR;

namespace CodeForge.Application.Leads.MarkLeadContacted
{
    public record MarkLeadContactedCommand(Guid Id) : IRequest<LeadDto>;
}
