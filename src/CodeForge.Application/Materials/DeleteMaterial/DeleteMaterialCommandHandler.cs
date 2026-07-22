using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Materials.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Materials.DeleteMaterial
{
    public class DeleteMaterialCommandHandler : IRequestHandler<DeleteMaterialCommand, MaterialDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeleteMaterialCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<MaterialDto> Handle(DeleteMaterialCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

            if (material is null)
            {
                throw new KeyNotFoundException("Material was not found.");
            }

            var course = await MaterialParentResolver.ResolveCourseAsync(
                _context, material.ModuleId, material.SessionId, cancellationToken);

            CourseContentAuthorization.EnsureCanManage(_currentUserService, course, currentUserId);

            _context.Materials.Remove(material);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "material.deleted", nameof(Material), material.Id, new { material.Title }));

            await _context.SaveChangesAsync(cancellationToken);

            return MaterialMapping.ToDto(material);
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
