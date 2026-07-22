using CodeForge.Application.Assignments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assignments.CreateAssignment
{
    public class CreateAssignmentCommandHandler : IRequestHandler<CreateAssignmentCommand, AssignmentResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateAssignmentCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AssignmentResponseDto> Handle(CreateAssignmentCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var module = await _context.Modules
                .Include(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(m => m.Assignments)
                .FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken);

            if (module is null)
            {
                throw new KeyNotFoundException("Module was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, module.Course, currentUserId);

            var maxOrder = module.Assignments.Count == 0 ? 0 : module.Assignments.Max(a => a.OrderIndex);

            var assignment = new Assignment
            {
                ModuleId = module.Id,
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                OrderIndex = maxOrder + 1,
                IsPractice = request.IsPractice,
                MaxAttempts = request.MaxAttempts,
                DueAt = request.DueAt,
                PassScore = request.PassScore,
            };

            _context.Assignments.Add(assignment);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "assignment.created", nameof(Assignment), assignment.Id,
                new { assignment.Title, moduleId = module.Id }));

            await _context.SaveChangesAsync(cancellationToken);

            return new AssignmentResponseDto(assignment.Id, "Assignment created.");
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
