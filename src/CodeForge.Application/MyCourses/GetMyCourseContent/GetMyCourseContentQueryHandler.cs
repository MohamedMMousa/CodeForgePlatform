using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.MyCourses.Common;
using CodeForge.Application.Sessions.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.MyCourses.GetMyCourseContent
{
    public class GetMyCourseContentQueryHandler : IRequestHandler<GetMyCourseContentQuery, MyCourseContentDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetMyCourseContentQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<MyCourseContentDto> Handle(
            GetMyCourseContentQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Instructors)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);

            if (course is null)
            {
                throw new KeyNotFoundException("Course was not found.");
            }

            CourseContentAuthorization.EnsureCanView(_currentUserService, course, currentUserId);

            var modules = await _context.Modules
                .AsNoTracking()
                .Include(m => m.Sessions).ThenInclude(s => s.Instructor)
                .Include(m => m.Sessions).ThenInclude(s => s.Materials)
                .Include(m => m.Quizzes)
                .Include(m => m.Assignments)
                .Where(m => m.CourseId == request.CourseId)
                .OrderBy(m => m.OrderIndex)
                .ToListAsync(cancellationToken);

            var moduleDtos = modules.Select(m => new MyCourseModuleDto(
                m.Id,
                m.Title,
                m.Description,
                m.OrderIndex,
                m.Sessions.OrderBy(s => s.OrderIndex).Select(SessionMapping.ToDto).ToList(),
                m.Quizzes.OrderBy(q => q.OrderIndex)
                    .Select(q => new MyCourseAssessmentDto(q.Id, q.Type, q.Title, q.TimeLimitMinutes, q.PassScore, q.MaxAttempts, q.IsPractice))
                    .ToList(),
                m.Assignments.OrderBy(a => a.OrderIndex)
                    .Select(a => new MyCourseAssignmentDto(a.Id, a.Title, a.DueAt, a.MaxAttempts, a.IsPractice))
                    .ToList()))
                .ToList();

            return new MyCourseContentDto(course.Id, course.Title, moduleDtos);
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
