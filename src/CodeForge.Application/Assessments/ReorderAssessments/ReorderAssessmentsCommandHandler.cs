using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assessments.ReorderAssessments
{
    public class ReorderAssessmentsCommandHandler : IRequestHandler<ReorderAssessmentsCommand>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ReorderAssessmentsCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task Handle(ReorderAssessmentsCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var module = await _context.Modules
                .Include(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(m => m.Quizzes)
                .FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken);

            if (module is null)
            {
                throw new KeyNotFoundException("Module was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, module.Course, currentUserId);

            var assessmentIds = request.AssessmentOrders.Select(x => x.AssessmentId).ToList();
            var invalidAssessments = assessmentIds.Except(module.Quizzes.Select(q => q.Id)).ToList();
            if (invalidAssessments.Count != 0)
            {
                throw new InvalidOperationException("One or more assessments do not belong to the specified module.");
            }

            var duplicateOrders = request.AssessmentOrders.GroupBy(x => x.OrderIndex).Where(g => g.Count() > 1).ToList();
            if (duplicateOrders.Count != 0)
            {
                throw new InvalidOperationException("Duplicate order indices are not allowed.");
            }

            foreach (var assessmentOrder in request.AssessmentOrders)
            {
                var quiz = module.Quizzes.First(q => q.Id == assessmentOrder.AssessmentId);
                quiz.OrderIndex = assessmentOrder.OrderIndex;
            }

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "assessments.reordered", nameof(Module), module.Id, new { moduleId = module.Id }));

            await _context.SaveChangesAsync(cancellationToken);
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
