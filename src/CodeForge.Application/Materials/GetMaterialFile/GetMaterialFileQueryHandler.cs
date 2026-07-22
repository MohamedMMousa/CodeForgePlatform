using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Materials.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Materials.GetMaterialFile
{
    public class GetMaterialFileQueryHandler : IRequestHandler<GetMaterialFileQuery, MaterialFileResult>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileStorageService _fileStorageService;

        public GetMaterialFileQueryHandler(
            ICodeForgeDbContext context,
            ICurrentUserService currentUserService,
            IFileStorageService fileStorageService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _fileStorageService = fileStorageService;
        }

        public async Task<MaterialFileResult> Handle(GetMaterialFileQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var material = await _context.Materials
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == request.MaterialId, cancellationToken);

            if (material is null || material.Type != MaterialTypes.File || material.FileUrl is null)
            {
                throw new KeyNotFoundException("Material file was not found.");
            }

            var course = await MaterialParentResolver.ResolveCourseAsync(
                _context, material.ModuleId, material.SessionId, cancellationToken);

            CourseContentAuthorization.EnsureCanView(_currentUserService, course, currentUserId);

            var (stream, contentType) = await _fileStorageService.OpenMaterialAsync(material.FileUrl, cancellationToken);
            return new MaterialFileResult(stream, contentType, material.Title);
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
