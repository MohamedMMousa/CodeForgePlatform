using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Sessions.ReorderSessions
{
    public class ReorderSessionsCommandHandler : IRequestHandler<ReorderSessionsCommand>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ReorderSessionsCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task Handle(ReorderSessionsCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var module = await _context.Modules
                .Include(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(m => m.Sessions)
                .FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken);

            if (module is null)
            {
                throw new KeyNotFoundException("Module was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, module.Course, currentUserId);

            var sessionIds = request.SessionOrders.Select(x => x.SessionId).ToList();
            var invalidSessions = sessionIds.Except(module.Sessions.Select(s => s.Id)).ToList();
            if (invalidSessions.Count != 0)
            {
                throw new InvalidOperationException("One or more sessions do not belong to the specified module.");
            }

            var duplicateOrders = request.SessionOrders.GroupBy(x => x.OrderIndex).Where(g => g.Count() > 1).ToList();
            if (duplicateOrders.Count != 0)
            {
                throw new InvalidOperationException("Duplicate order indices are not allowed.");
            }

            foreach (var sessionOrder in request.SessionOrders)
            {
                var session = module.Sessions.First(s => s.Id == sessionOrder.SessionId);
                session.OrderIndex = sessionOrder.OrderIndex;
            }

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "sessions.reordered", nameof(Module), module.Id, new { moduleId = module.Id }));

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
