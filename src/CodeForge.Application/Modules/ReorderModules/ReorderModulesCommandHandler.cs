using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Modules.ReorderModules
{
    public class ReorderModulesCommandHandler : IRequestHandler<ReorderModulesCommand>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ReorderModulesCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task Handle(ReorderModulesCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var course = await _context.Courses
                .Include(c => c.Instructors)
                .Include(c => c.Modules)
                .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);

            if (course is null)
            {
                throw new KeyNotFoundException("Course was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, course, currentUserId);

            var moduleIds = request.ModuleOrders.Select(x => x.ModuleId).ToList();
            var invalidModules = moduleIds.Except(course.Modules.Select(m => m.Id)).ToList();
            if (invalidModules.Count != 0)
            {
                throw new InvalidOperationException("One or more modules do not belong to the specified course.");
            }

            var duplicateOrders = request.ModuleOrders.GroupBy(x => x.OrderIndex).Where(g => g.Count() > 1).ToList();
            if (duplicateOrders.Count != 0)
            {
                throw new InvalidOperationException("Duplicate order indices are not allowed.");
            }

            foreach (var moduleOrder in request.ModuleOrders)
            {
                var module = course.Modules.First(m => m.Id == moduleOrder.ModuleId);
                module.OrderIndex = moduleOrder.OrderIndex;
            }

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "modules.reordered", nameof(Course), course.Id, new { courseId = course.Id }));

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
