using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Courses.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Courses.CreateCourse
{
    public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, CourseDetailDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateCourseCommandHandler(
            ICodeForgeDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CourseDetailDto> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var slug = request.Slug.Trim().ToLower();

            var slugExists = await _context.Courses
                .AnyAsync(x => x.Slug == slug, cancellationToken);

            if (slugExists)
            {
                throw new InvalidOperationException("Course slug is already in use.");
            }

            var course = new Course
            {
                Title = request.Title.Trim(),
                Slug = slug,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                ThumbnailUrl = string.IsNullOrWhiteSpace(request.ThumbnailUrl) ? null : request.ThumbnailUrl.Trim(),
                Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
                Price = request.Price,
                Currency = request.Currency.Trim().ToUpper(),
                Status = CourseStatuses.Draft,
                CreatedById = adminId
            };

            _context.Courses.Add(course);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId,
                "course.created",
                course.Id,
                new
                {
                    course.Title,
                    course.Slug,
                    course.Price,
                    course.Currency
                }));

            await _context.SaveChangesAsync(cancellationToken);

            course.CreatedBy = await _context.Users
                .AsNoTracking()
                .FirstAsync(x => x.Id == adminId, cancellationToken);

            return CourseMapping.ToDetailDto(course);
        }

        private Guid GetCurrentUserId()
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated admin could not be resolved.");
            }

            return userId;
        }
    }
}
