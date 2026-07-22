using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Courses.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Courses.RemoveInstructorFromCourse
{
    public class RemoveInstructorFromCourseCommandHandler
        : IRequestHandler<RemoveInstructorFromCourseCommand, CourseMutationResultDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public RemoveInstructorFromCourseCommandHandler(
            ICodeForgeDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CourseMutationResultDto> Handle(
            RemoveInstructorFromCourseCommand request,
            CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var assignment = await _context.CourseInstructors
                .Include(x => x.Course)
                .Include(x => x.Instructor)
                .FirstOrDefaultAsync(
                    x => x.CourseId == request.CourseId &&
                         x.InstructorId == request.InstructorId,
                    cancellationToken);

            if (assignment is null)
            {
                throw new KeyNotFoundException("Instructor assignment was not found.");
            }

            _context.CourseInstructors.Remove(assignment);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId,
                "course.instructor_removed",
                assignment.CourseId,
                new
                {
                    assignment.Course.Title,
                    instructorId = assignment.InstructorId,
                    assignment.Instructor.Email
                }));

            await _context.SaveChangesAsync(cancellationToken);

            return new CourseMutationResultDto(request.CourseId, "Instructor removed from course.");
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
