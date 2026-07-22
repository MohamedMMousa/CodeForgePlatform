using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Courses.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Courses.UpdateCourse
{
    public class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand, CourseDetailDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateCourseCommandHandler(
            ICodeForgeDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CourseDetailDto> Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var slug = request.Slug.Trim().ToLower();

            var course = await _context.Courses
                .Include(x => x.CreatedBy)
                .Include(x => x.Instructors)
                .ThenInclude(x => x.Instructor)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (course is null)
            {
                throw new KeyNotFoundException("Course was not found.");
            }

            var slugExists = await _context.Courses
                .AnyAsync(x => x.Id != request.Id && x.Slug == slug, cancellationToken);

            if (slugExists)
            {
                throw new InvalidOperationException("Course slug is already in use.");
            }

            course.Title = request.Title.Trim();
            course.Slug = slug;
            course.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            course.ThumbnailUrl = string.IsNullOrWhiteSpace(request.ThumbnailUrl) ? null : request.ThumbnailUrl.Trim();
            course.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
            course.Price = request.Price;
            course.Currency = request.Currency.Trim().ToUpper();

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId,
                "course.updated",
                course.Id,
                new
                {
                    course.Title,
                    course.Slug,
                    course.Price,
                    course.Currency
                }));

            await _context.SaveChangesAsync(cancellationToken);

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
