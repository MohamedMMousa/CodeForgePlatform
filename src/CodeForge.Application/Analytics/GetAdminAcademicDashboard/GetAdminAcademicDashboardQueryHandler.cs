using CodeForge.Application.Analytics.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Analytics.GetAdminAcademicDashboard
{
    public class GetAdminAcademicDashboardQueryHandler
        : IRequestHandler<GetAdminAcademicDashboardQuery, AdminAcademicDashboardDto>
    {
        private readonly ICodeForgeDbContext _context;

        public GetAdminAcademicDashboardQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<AdminAcademicDashboardDto> Handle(GetAdminAcademicDashboardQuery request, CancellationToken cancellationToken)
        {
            var certificatesIssued = await _context.Certificates.CountAsync(cancellationToken);
            var completion = await _context.Certificates.CountAsync(c => c.Tier == CertificateTiers.Completion, cancellationToken);
            var participation = await _context.Certificates.CountAsync(c => c.Tier == CertificateTiers.Participation, cancellationToken);
            var revoked = await _context.Certificates.CountAsync(c => c.IsRevoked, cancellationToken);

            var totalAssessments = await _context.Quizzes.CountAsync(cancellationToken);
            var submittedAttempts = await _context.QuizAttempts.CountAsync(a => a.SubmittedAt != null, cancellationToken);
            var passedAttempts = await _context.QuizAttempts.CountAsync(a => a.SubmittedAt != null && a.Passed == true, cancellationToken);
            var totalAssignments = await _context.Assignments.CountAsync(cancellationToken);
            var totalSubmissions = await _context.AssignmentSubmissions.CountAsync(cancellationToken);

            var courses = await _context.Courses
                .AsNoTracking()
                .Select(c => new { c.Id, c.Title })
                .ToListAsync(cancellationToken);

            var activeByCourse = (await _context.Enrollments
                .Where(e => e.Status == EnrollmentStatuses.Active)
                .GroupBy(e => e.CourseId)
                .Select(g => new { CourseId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.CourseId, x => x.Count);

            var assessmentsByCourse = (await _context.Quizzes
                .GroupBy(q => q.Module.CourseId)
                .Select(g => new { CourseId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.CourseId, x => x.Count);

            var attemptsByCourse = (await _context.QuizAttempts
                .Where(a => a.SubmittedAt != null)
                .GroupBy(a => a.Quiz.Module.CourseId)
                .Select(g => new { CourseId = g.Key, Submitted = g.Count(), Passed = g.Count(x => x.Passed == true) })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.CourseId, x => (x.Submitted, x.Passed));

            var certsByCourse = (await _context.Certificates
                .GroupBy(c => c.CourseId)
                .Select(g => new { CourseId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.CourseId, x => x.Count);

            var rows = courses
                .Select(c =>
                {
                    var attempts = attemptsByCourse.GetValueOrDefault(c.Id);
                    return new CourseAcademicRowDto(
                        c.Id,
                        c.Title,
                        activeByCourse.GetValueOrDefault(c.Id),
                        assessmentsByCourse.GetValueOrDefault(c.Id),
                        attempts.Submitted,
                        AnalyticsCalculator.PassRate(attempts.Submitted, attempts.Passed),
                        certsByCourse.GetValueOrDefault(c.Id));
                })
                .OrderByDescending(r => r.ActiveEnrollments)
                .ThenBy(r => r.Title)
                .ToList();

            return new AdminAcademicDashboardDto(
                certificatesIssued,
                completion,
                participation,
                revoked,
                totalAssessments,
                submittedAttempts,
                AnalyticsCalculator.PassRate(submittedAttempts, passedAttempts),
                totalAssignments,
                totalSubmissions,
                rows);
        }
    }
}
