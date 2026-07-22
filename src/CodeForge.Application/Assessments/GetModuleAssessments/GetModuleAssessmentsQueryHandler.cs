using CodeForge.Application.Assessments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assessments.GetModuleAssessments
{
    public class GetModuleAssessmentsQueryHandler : IRequestHandler<GetModuleAssessmentsQuery, IReadOnlyList<AssessmentDto>>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetModuleAssessmentsQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IReadOnlyList<AssessmentDto>> Handle(GetModuleAssessmentsQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var module = await _context.Modules
                .Include(m => m.Course).ThenInclude(c => c.Instructors)
                .FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken);

            if (module is null)
            {
                throw new KeyNotFoundException("Module was not found.");
            }

            // Manage view exposes IsCorrect on options — instructor/admin only, never
            // students (EnsureCanView would leak answers before an attempt).
            CourseContentAuthorization.EnsureCanManage(_currentUserService, module.Course, currentUserId);

            var quizzes = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Questions)
                .Where(q => q.ModuleId == request.ModuleId)
                .OrderBy(q => q.OrderIndex)
                .ToListAsync(cancellationToken);

            return quizzes.Select(AssessmentMapping.ToDto).ToList();
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
