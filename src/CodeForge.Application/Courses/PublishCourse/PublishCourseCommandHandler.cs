using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Courses.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Courses.PublishCourse
{
    public class PublishCourseCommandHandler : IRequestHandler<PublishCourseCommand, CourseMutationResultDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public PublishCourseCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CourseMutationResultDto> Handle(PublishCourseCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var course = await _context.Courses.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (course is null)
            {
                throw new KeyNotFoundException("Course was not found.");
            }

            if (course.Status == CourseStatuses.Published)
            {
                return new CourseMutationResultDto(course.Id, "Course is already published.");
            }

            course.Status = CourseStatuses.Published;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId,
                "course.published",
                course.Id,
                new { course.Title, course.Slug }));

            await _context.SaveChangesAsync(cancellationToken);

            return new CourseMutationResultDto(course.Id, "Course published.");
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
