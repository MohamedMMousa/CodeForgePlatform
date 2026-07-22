using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Materials.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Materials.GetSessionMaterials
{
    public class GetSessionMaterialsQueryHandler : IRequestHandler<GetSessionMaterialsQuery, IReadOnlyList<MaterialDto>>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetSessionMaterialsQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IReadOnlyList<MaterialDto>> Handle(
            GetSessionMaterialsQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var course = await MaterialParentResolver.ResolveCourseAsync(
                _context, null, request.SessionId, cancellationToken);

            CourseContentAuthorization.EnsureCanView(_currentUserService, course, currentUserId);

            var materials = await _context.Materials
                .AsNoTracking()
                .Where(m => m.SessionId == request.SessionId)
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
