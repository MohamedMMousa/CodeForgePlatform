using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Materials.Common;
using CodeForge.Domain.Entities;
using MediatR;

namespace CodeForge.Application.Materials.CreateMaterial
{
    public class CreateMaterialCommandHandler : IRequestHandler<CreateMaterialCommand, MaterialDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileStorageService _fileStorageService;

        public CreateMaterialCommandHandler(
            ICodeForgeDbContext context,
            ICurrentUserService currentUserService,
            IFileStorageService fileStorageService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _fileStorageService = fileStorageService;
        }

        public async Task<MaterialDto> Handle(CreateMaterialCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var course = await MaterialParentResolver.ResolveCourseAsync(
                _context, request.ModuleId, request.SessionId, cancellationToken);

            CourseContentAuthorization.EnsureCanManage(_currentUserService, course, currentUserId);

            var material = new Material
            {
                ModuleId = request.ModuleId,
                SessionId = request.SessionId,
                Type = request.Type,
                Title = request.Title.Trim(),
                Body = string.IsNullOrWhiteSpace(request.Body) ? null : request.Body.Trim(),
                LinkUrl = string.IsNullOrWhiteSpace(request.LinkUrl) ? null : request.LinkUrl.Trim()
            };

            if (request.Type == MaterialTypes.File)
            {
                var (url, sizeKb) = await _fileStorageService.SaveCourseMaterialAsync(
                    request.FileStream!, request.FileName!, request.ContentType!, cancellationToken);
                material.FileUrl = url;
                material.FileType = request.FileType;
                material.FileSizeKb = sizeKb;
            }

            _context.Materials.Add(material);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "material.created", nameof(Material), material.Id,
                new { material.Title, material.Type }));

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
