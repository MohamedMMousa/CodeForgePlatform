using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Modules.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Modules.CreateModule
{
    public class CreateModuleCommandHandler : IRequestHandler<CreateModuleCommand, ModuleResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateModuleCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ModuleResponseDto> Handle(CreateModuleCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var course = await _context.Courses
                .Include(c => c.Instructors)
                .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);

            if (course is null)
            {
                throw new KeyNotFoundException("Course was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, course, currentUserId);

            var maxOrder = await _context.Modules
                .Where(m => m.CourseId == request.CourseId)
                .MaxAsync(m => (int?)m.OrderIndex, cancellationToken) ?? 0;

            var module = new Module
            {
                CourseId = request.CourseId,
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                OrderIndex = maxOrder + 1
            };

            _context.Modules.Add(module);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "module.created", nameof(Module), module.Id,
                new { module.Title, courseId = request.CourseId }));

            await _context.SaveChangesAsync(cancellationToken);

            return new ModuleResponseDto(module.Id, "Module created.");
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
