using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Leads.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Leads.MarkLeadContacted
{
    public class MarkLeadContactedCommandHandler : IRequestHandler<MarkLeadContactedCommand, LeadDto>
    {
        private readonly ICodeForgeDbContext _context;

        public MarkLeadContactedCommandHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<LeadDto> Handle(MarkLeadContactedCommand request, CancellationToken cancellationToken)
        {
            var lead = await _context.Leads
                .Include(x => x.Course)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (lead is null)
            {
                throw new KeyNotFoundException("Lead was not found.");
            }

            lead.IsContacted = true;
            await _context.SaveChangesAsync(cancellationToken);

            return new LeadDto(
                lead.Id, lead.Name, lead.Email, lead.Phone, lead.Message,
                lead.CourseId, lead.Course?.Title, lead.IsContacted, lead.CreatedAt);
        }
    }
}
