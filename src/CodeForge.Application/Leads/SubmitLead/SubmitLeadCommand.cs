using CodeForge.Application.Leads.Common;
using MediatR;

namespace CodeForge.Application.Leads.SubmitLead
{
    public record SubmitLeadCommand(
        string Name,
        string Email,
        string? Phone,
        string? Message,
        Guid? CourseId) : IRequest<LeadDto>;
}
