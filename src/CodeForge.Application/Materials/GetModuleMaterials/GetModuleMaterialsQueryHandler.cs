using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Materials.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Materials.GetModuleMaterials
{
    public class GetModuleMaterialsQueryHandler : IRequestHandler<GetModuleMaterialsQuery, IReadOnlyList<MaterialDto>>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetModuleMaterialsQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IReadOnlyList<MaterialDto>> Handle(
            GetModuleMaterialsQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var course = await MaterialParentResolver.ResolveCourseAsync(
                _context, request.ModuleId, null, cancellationToken);

            CourseContentAuthorization.EnsureCanView(_currentUserService, course, currentUserId);

            var materials = await _context.Materials
                .AsNoTracking()
                .Where(m => m.ModuleId == request.ModuleId)
                .OrderBy(m => m.OrderIndex)
                .ToListAsync(cancellationToken);

            return materials.Select(MaterialMapping.ToDto).ToList();
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
