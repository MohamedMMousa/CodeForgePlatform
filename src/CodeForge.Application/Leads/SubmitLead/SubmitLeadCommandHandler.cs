using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Leads.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Leads.SubmitLead
{
    public class SubmitLeadCommandHandler : IRequestHandler<SubmitLeadCommand, LeadDto>
    {
        private readonly ICodeForgeDbContext _context;

        public SubmitLeadCommandHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<LeadDto> Handle(SubmitLeadCommand request, CancellationToken cancellationToken)
        {
            Course? course = null;
            if (request.CourseId.HasValue)
            {
                course = await _context.Courses
                    .FirstOrDefaultAsync(x => x.Id == request.CourseId.Value, cancellationToken);
                if (course is null)
                {
                    throw new KeyNotFoundException("Selected course was not found.");
                }
            }

            var lead = new Lead
            {
                Name = request.Name.Trim(),
                Email = request.Email.Trim().ToLower(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
                CourseId = course?.Id
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync(cancellationToken);

            return new LeadDto(
                lead.Id, lead.Name, lead.Email, lead.Phone, lead.Message,
                course?.Id, course?.Title, lead.IsContacted, lead.CreatedAt);
        }
    }
}
