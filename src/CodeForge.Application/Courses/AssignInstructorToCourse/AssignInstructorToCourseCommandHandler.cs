using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Courses.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Courses.AssignInstructorToCourse
{
    public class AssignInstructorToCourseCommandHandler
        : IRequestHandler<AssignInstructorToCourseCommand, CourseInstructorDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AssignInstructorToCourseCommandHandler(
            ICodeForgeDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CourseInstructorDto> Handle(
            AssignInstructorToCourseCommand request,
            CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var course = await _context.Courses
                .FirstOrDefaultAsync(x => x.Id == request.CourseId, cancellationToken);

            if (course is null)
            {
                throw new KeyNotFoundException("Course was not found.");
            }

            var instructor = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == request.InstructorId, cancellationToken);

            if (instructor is null || !instructor.IsActive || instructor.Role != Roles.Instructor)
            {
                throw new InvalidOperationException("Active instructor was not found.");
            }

            var assignmentExists = await _context.CourseInstructors
                .AnyAsync(
                    x => x.CourseId == request.CourseId &&
                         x.InstructorId == request.InstructorId,
                    cancellationToken);

            if (assignmentExists)
            {
                throw new InvalidOperationException("Instructor is already assigned to this course.");
            }

            var assignment = new CourseInstructor
            {
                CourseId = course.Id,
                InstructorId = instructor.Id,
                Instructor = instructor,
                Course = course
            };

            _context.CourseInstructors.Add(assignment);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId,
                "course.instructor_assigned",
                course.Id,
                new
                {
                    course.Title,
                    instructorId = instructor.Id,
                    instructor.Email
                }));

            await _context.SaveChangesAsync(cancellationToken);

            return CourseMapping.ToInstructorDto(assignment);
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
