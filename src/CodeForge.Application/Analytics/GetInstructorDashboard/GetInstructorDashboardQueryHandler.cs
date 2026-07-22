using CodeForge.Application.Analytics.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Analytics.GetInstructorDashboard
{
    public class GetInstructorDashboardQueryHandler
        : IRequestHandler<GetInstructorDashboardQuery, InstructorDashboardDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetInstructorDashboardQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<InstructorDashboardDto> Handle(GetInstructorDashboardQuery request, CancellationToken cancellationToken)
        {
            var instructorId = GetCurrentUserId();

            var courses = await _context.Courses
                .AsNoTracking()
                .Where(c => c.Instructors.Any(i => i.InstructorId == instructorId))
                .Select(c => new { c.Id, c.Title, c.Status })
                .ToListAsync(cancellationToken);
            var courseIds = courses.Select(c => c.Id).ToList();

            var activeEnrollments = await _context.Enrollments
                .Where(e => courseIds.Contains(e.CourseId) && e.Status == EnrollmentStatuses.Active)
                .Select(e => new { e.CourseId, e.StudentId })
                .ToListAsync(cancellationToken);
            var activeByCourse = activeEnrollments
                .GroupBy(e => e.CourseId)
                .ToDictionary(g => g.Key, g => g.Count());
            var totalActiveStudents = activeEnrollments.Select(e => e.StudentId).Distinct().Count();

            var assessmentsByCourse = (await _context.Quizzes
                .Where(q => courseIds.Contains(q.Module.CourseId))
                .GroupBy(q => q.Module.CourseId)
                .Select(g => new { CourseId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.CourseId, x => x.Count);

            var attemptsByCourse = (await _context.QuizAttempts
                .Where(a => a.SubmittedAt != null && courseIds.Contains(a.Quiz.Module.CourseId))
                .GroupBy(a => a.Quiz.Module.CourseId)
                .Select(g => new { CourseId = g.Key, Submitted = g.Count(), Passed = g.Count(x => x.Passed == true) })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.CourseId, x => (x.Submitted, x.Passed));

            var certsByCourse = (await _context.Certificates
                .Where(c => courseIds.Contains(c.CourseId))
                .GroupBy(c => c.CourseId)
                .Select(g => new { CourseId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.CourseId, x => x.Count);

            var rows = courses
                .Select(c =>
                {
                    var attempts = attemptsByCourse.GetValueOrDefault(c.Id);
                    return new InstructorCourseRowDto(
                        c.Id,
                        c.Title,
                        c.Status,
                        activeByCourse.GetValueOrDefault(c.Id),
                        assessmentsByCourse.GetValueOrDefault(c.Id),
                        attempts.Submitted,
                        AnalyticsCalculator.PassRate(attempts.Submitted, attempts.Passed),
                        certsByCourse.GetValueOrDefault(c.Id));
                })
                .OrderBy(r => r.Title)
                .ToList();

            return new InstructorDashboardDto(
                courses.Count,
                totalActiveStudents,
                certsByCourse.Values.Sum(),
                rows);
        }

        private Guid GetCurrentUserId()
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated instructor could not be resolved.");
            }

            return userId;
        }
    }
}
