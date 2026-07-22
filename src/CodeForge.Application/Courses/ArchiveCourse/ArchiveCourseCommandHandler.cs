using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Courses.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Courses.ArchiveCourse
{
    public class ArchiveCourseCommandHandler : IRequestHandler<ArchiveCourseCommand, CourseMutationResultDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ArchiveCourseCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CourseMutationResultDto> Handle(ArchiveCourseCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var course = await _context.Courses.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (course is null)
            {
                throw new KeyNotFoundException("Course was not found.");
            }

            if (course.Status == CourseStatuses.Archived)
            {
                return new CourseMutationResultDto(course.Id, "Course is already archived.");
            }

            course.Status = CourseStatuses.Archived;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId,
                "course.archived",
                course.Id,
                new { course.Title, course.Slug }));

            await _context.SaveChangesAsync(cancellationToken);

            return new CourseMutationResultDto(course.Id, "Course archived.");
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
