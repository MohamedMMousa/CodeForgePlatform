using CodeForge.Application.Analytics.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Analytics.GetAdminBusinessDashboard
{
    public class GetAdminBusinessDashboardQueryHandler
        : IRequestHandler<GetAdminBusinessDashboardQuery, AdminBusinessDashboardDto>
    {
        private readonly ICodeForgeDbContext _context;

        public GetAdminBusinessDashboardQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<AdminBusinessDashboardDto> Handle(GetAdminBusinessDashboardQuery request, CancellationToken cancellationToken)
        {
            var totalStudents = await _context.Users.CountAsync(u => u.Role == Roles.Student, cancellationToken);
            var publishedCourses = await _context.Courses.CountAsync(c => c.Status == CourseStatuses.Published, cancellationToken);
            var publishedTracks = await _context.Tracks.CountAsync(t => t.Status == TrackStatuses.Published, cancellationToken);
            var activeEnrollments = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentStatuses.Active, cancellationToken);
            var pendingRequests = await _context.EnrollmentRequests.CountAsync(r => r.Status == EnrollmentRequestStatuses.Pending, cancellationToken);
            var totalLeads = await _context.Leads.CountAsync(cancellationToken);
            var uncontactedLeads = await _context.Leads.CountAsync(l => !l.IsContacted, cancellationToken);
            var openCohorts = await _context.Cohorts.CountAsync(c => c.Status == CohortStatuses.Open, cancellationToken);

            var totalRevenue = await _context.EnrollmentRequests
                .Where(r => r.Status == EnrollmentRequestStatuses.Approved)
                .SumAsync(r => (decimal?)r.FinalPrice, cancellationToken) ?? 0m;

            var topCoursesByRevenue = await _context.EnrollmentRequests
                .Where(r => r.Status == EnrollmentRequestStatuses.Approved && r.CourseId != null)
                .GroupBy(r => new { r.CourseId, r.Course!.Title })
                .Select(g => new RevenueByCourseDto(
                    g.Key.CourseId!.Value,
                    g.Key.Title,
                    g.Sum(r => r.FinalPrice),
                    g.Count()))
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync(cancellationToken);

            // Enrollments per month for the last 6 calendar months (small, bounded set → group in memory).
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var recentEnrollmentDates = await _context.Enrollments
                .Where(e => e.CreatedAt >= sixMonthsAgo)
                .Select(e => e.CreatedAt)
                .ToListAsync(cancellationToken);
            var enrollmentsByMonth = recentEnrollmentDates
                .GroupBy(d => new { d.Year, d.Month })
                .Select(g => new MonthlyCountDto(g.Key.Year, g.Key.Month, g.Count()))
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            return new AdminBusinessDashboardDto(
                totalStudents,
                publishedCourses,
                publishedTracks,
                activeEnrollments,
                pendingRequests,
                totalRevenue,
                totalLeads,
                uncontactedLeads,
                openCohorts,
                topCoursesByRevenue,
                enrollmentsByMonth);
        }
    }
}
