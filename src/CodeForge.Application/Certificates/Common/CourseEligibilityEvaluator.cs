using CodeForge.Application.Attendance.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Gradebook.Common;
using CodeForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Certificates.Common
{
    /// <summary>
    /// Computes two-tier certificate eligibility for a course's active enrollments,
    /// reusing the exact attendance-rate and assessment-pass logic the gradebook uses
    /// (<see cref="AttendanceRateCalculator"/> + <see cref="GradebookCalculator"/>), so a
    /// certificate can never disagree with the gradebook. Shared by the candidate-list
    /// query and the issue command. The caller is responsible for authorization on the
    /// returned course.
    /// </summary>
    public static class CourseEligibilityEvaluator
    {
        public record EnrollmentEligibility(
            Enrollment Enrollment,
            decimal AttendanceRate,
            int RequiredAssessmentCount,
            CertificateEligibilityCalculator.Result Result);

        public record CourseEvaluation(Course Course, IReadOnlyList<EnrollmentEligibility> Enrollments);

        // A certificate can be issued to anyone who was genuinely enrolled — currently
        // active or whose access has simply expired at term end — but never to a
        // cancelled/refunded enrollment.
        private static readonly string[] CertifiableStatuses = { EnrollmentStatuses.Active, EnrollmentStatuses.Expired };

        public static async Task<CourseEvaluation?> EvaluateAsync(
            ICodeForgeDbContext context,
            Guid courseId,
            CancellationToken cancellationToken)
        {
            var course = await context.Courses
                .AsNoTracking()
                .Include(c => c.Instructors)
                .Include(c => c.Enrollments).ThenInclude(e => e.Student)
                .Include(c => c.Enrollments).ThenInclude(e => e.Cohort)
                .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

            if (course is null)
            {
                return null;
            }

            var quizzes = await context.Quizzes
                .AsNoTracking()
                .Where(q => q.Module.CourseId == courseId)
                .ToListAsync(cancellationToken);
            var quizIds = quizzes.Select(q => q.Id).ToList();
            var attempts = await context.QuizAttempts
                .AsNoTracking()
                .Where(a => quizIds.Contains(a.QuizId))
                .ToListAsync(cancellationToken);

            var sessions = await context.Sessions
                .AsNoTracking()
                .Where(s => s.Module.CourseId == courseId
                    && (s.Type == SessionTypes.Live || s.Type == SessionTypes.InPerson)
                    && s.ScheduledAt != null)
                .ToListAsync(cancellationToken);
            var sessionIds = sessions.Select(s => s.Id).ToList();
            var attendanceRecords = await context.AttendanceRecords
                .AsNoTracking()
                .Where(a => sessionIds.Contains(a.SessionId))
                .ToListAsync(cancellationToken);

            // The completion bar is judged only on graded (non-practice) assessments.
            var requiredQuizzes = quizzes.Where(q => !q.IsPractice).ToList();

            var now = DateTime.UtcNow;
            var results = new List<EnrollmentEligibility>();

            foreach (var enrollment in course.Enrollments.Where(e => CertifiableStatuses.Contains(e.Status)))
            {
                var windowStart = enrollment.Cohort.StartDate;
                var windowEnd = enrollment.Cohort.EndDate.AddDays(enrollment.Cohort.GracePeriodDays);

                var heldSessionIds = sessions
                    .Where(s => s.ScheduledAt!.Value >= windowStart && s.ScheduledAt.Value <= windowEnd && s.ScheduledAt.Value <= now)
                    .Select(s => s.Id)
                    .ToHashSet();

                var statuses = attendanceRecords
                    .Where(a => a.StudentId == enrollment.StudentId && heldSessionIds.Contains(a.SessionId))
                    .Select(a => a.Status)
                    .ToList();

                var attendanceRate = AttendanceRateCalculator.Calculate(heldSessionIds.Count, statuses).Rate;

                var assessmentGrades = GradebookCalculator.BuildAssessmentGrades(enrollment.StudentId, requiredQuizzes, attempts);
                var passStates = assessmentGrades.Select(g => g.Passed == true).ToList();

                var result = CertificateEligibilityCalculator.Evaluate(
                    attendanceRate, course.CompletionAttendanceThreshold, passStates);

                results.Add(new EnrollmentEligibility(enrollment, attendanceRate, requiredQuizzes.Count, result));
            }

            return new CourseEvaluation(course, results);
        }
    }
}
