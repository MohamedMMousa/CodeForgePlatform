using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Modules.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Modules.UpdateModule
{
    public class UpdateModuleCommandHandler : IRequestHandler<UpdateModuleCommand, ModuleResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateModuleCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ModuleResponseDto> Handle(UpdateModuleCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var module = await _context.Modules
                .Include(m => m.Course).ThenInclude(c => c.Instructors)
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

            if (module is null)
            {
                throw new KeyNotFoundException("Module was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, module.Course, currentUserId);

            module.Title = request.Title.Trim();
            module.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "module.updated", nameof(Module), module.Id, new { module.Title }));

            await _context.SaveChangesAsync(cancellationToken);

            return new ModuleResponseDto(module.Id, "Module updated.");
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
