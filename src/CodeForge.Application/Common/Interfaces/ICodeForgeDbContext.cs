using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CodeForge.Domain.Entities;

namespace CodeForge.Application.Common.Interfaces
{
    public interface ICodeForgeDbContext
    {
        DbSet<User> Users { get; }
        DbSet<PasswordResetToken> PasswordResetTokens { get; }
        DbSet<Course> Courses { get; }
        DbSet<CourseInstructor> CourseInstructors { get; }
        DbSet<Module> Modules { get; }
        DbSet<Session> Sessions { get; }
        DbSet<Material> Materials { get; }
        DbSet<Track> Tracks { get; }
        DbSet<TrackCourse> TrackCourses { get; }
        DbSet<Cohort> Cohorts { get; }
        DbSet<Coupon> Coupons { get; }
        DbSet<EnrollmentRequest> EnrollmentRequests { get; }
        DbSet<EnrollmentRequestCohort> EnrollmentRequestCohorts { get; }
        DbSet<Enrollment> Enrollments { get; }
        DbSet<SessionProgress> SessionProgresses { get; }
        DbSet<Quiz> Quizzes { get; }
        DbSet<QuizQuestion> QuizQuestions { get; }
        DbSet<QuizOption> QuizOptions { get; }
        DbSet<QuizAttempt> QuizAttempts { get; }
        DbSet<QuizAnswer> QuizAnswers { get; }
        DbSet<AttendanceRecord> AttendanceRecords { get; }
        DbSet<Assignment> Assignments { get; }
        DbSet<AssignmentTestCase> AssignmentTestCases { get; }
        DbSet<AssignmentSubmission> AssignmentSubmissions { get; }
        DbSet<AssignmentTestResult> AssignmentTestResults { get; }
        DbSet<Announcement> Announcements { get; }
        DbSet<Lead> Leads { get; }
        DbSet<ActivityLog> ActivityLogs { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
