using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Modules.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Modules.GetModuleById
{
    public class GetModuleByIdQueryHandler : IRequestHandler<GetModuleByIdQuery, ModuleListDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetModuleByIdQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ModuleListDto> Handle(GetModuleByIdQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var module = await _context.Modules
                .AsNoTracking()
                .Include(m => m.Sessions)
                .Include(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(m => m.Course).ThenInclude(c => c.Enrollments)
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

            if (module is null)
            {
                throw new KeyNotFoundException("Module was not found.");
            }

            CourseContentAuthorization.EnsureCanView(_currentUserService, module.Course, currentUserId);

            return ModuleMapping.ToDto(module);
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
