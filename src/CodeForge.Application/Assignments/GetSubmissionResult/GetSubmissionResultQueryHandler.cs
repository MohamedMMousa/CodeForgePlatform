using CodeForge.Application.Assignments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assignments.GetSubmissionResult
{
    public class GetSubmissionResultQueryHandler : IRequestHandler<GetSubmissionResultQuery, SubmissionResultDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetSubmissionResultQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<SubmissionResultDto> Handle(GetSubmissionResultQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var submission = await _context.AssignmentSubmissions
                .AsNoTracking()
                .Include(s => s.Assignment).ThenInclude(a => a.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(s => s.TestResults).ThenInclude(r => r.TestCase)
                .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, cancellationToken);

            if (submission is null)
            {
                throw new KeyNotFoundException("Submission was not found.");
            }

            if (submission.StudentId != currentUserId)
            {
                CourseContentAuthorization.EnsureCanManage(_currentUserService, submission.Assignment.Module.Course, currentUserId);
            }

            return SubmissionResultMapping.ToDto(submission);
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
