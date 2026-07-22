using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CodeForge.Domain.Entities
{
    // ============================================================================
    // 1. IDENTITY & AUTH DOMAIN
    // ============================================================================

    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string Role { get; set; } = null!; // admin, instructor, student
        public bool IsActive { get; set; } = true;
        public bool MustChangePassword { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Navigation properties
        public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
        public virtual ICollection<Course> CreatedCourses { get; set; } = new List<Course>();
        public virtual ICollection<CourseInstructor> CourseAssignments { get; set; } = new List<CourseInstructor>();
        public virtual ICollection<EnrollmentRequest> ReviewedEnrollmentRequests { get; set; } = new List<EnrollmentRequest>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<SessionProgress> SessionProgresses { get; set; } = new List<SessionProgress>();
        public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
        public virtual ICollection<Session> InstructedSessions { get; set; } = new List<Session>();
        public virtual ICollection<Announcement> AuthoredAnnouncements { get; set; } = new List<Announcement>();
        public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
        public virtual ICollection<Track> CreatedTracks { get; set; } = new List<Track>();
        public virtual ICollection<Coupon> CreatedCoupons { get; set; } = new List<Coupon>();
        public virtual ICollection<Enrollment> CancelledEnrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public virtual ICollection<AttendanceRecord> MarkedAttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public virtual ICollection<AssignmentSubmission> AssignmentSubmissions { get; set; } = new List<AssignmentSubmission>();
        public virtual ICollection<AssignmentSubmission> GradedAssignmentSubmissions { get; set; } = new List<AssignmentSubmission>();
    }

    public class PasswordResetToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }

    // ============================================================================
    // 2. COURSE STRUCTURE DOMAIN
    // ============================================================================

    public class Course
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Category { get; set; }
        public decimal Price { get; set; } = 0.00m;
        public string Currency { get; set; } = "EGP";
        public string Status { get; set; } = "draft"; // draft, published, archived
        public Guid CreatedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual User CreatedBy { get; set; } = null!;
        public virtual ICollection<CourseInstructor> Instructors { get; set; } = new List<CourseInstructor>();
        public virtual ICollection<Module> Modules { get; set; } = new List<Module>();
        public virtual ICollection<EnrollmentRequest> EnrollmentRequests { get; set; } = new List<EnrollmentRequest>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
        public virtual ICollection<TrackCourse> TrackCourses { get; set; } = new List<TrackCourse>();
        public virtual ICollection<Cohort> Cohorts { get; set; } = new List<Cohort>();
    }

    // ============================================================================
    // 2b. TRACKS (course bundles)
    // ============================================================================

    public class Track
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public decimal Price { get; set; } = 0.00m;
        public string Currency { get; set; } = "EGP";
        public string Status { get; set; } = "draft"; // draft, published, archived
        public Guid CreatedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual User CreatedBy { get; set; } = null!;
        public virtual ICollection<TrackCourse> TrackCourses { get; set; } = new List<TrackCourse>();
        public virtual ICollection<EnrollmentRequest> EnrollmentRequests { get; set; } = new List<EnrollmentRequest>();
    }

    public class TrackCourse
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TrackId { get; set; }
        public Guid CourseId { get; set; }
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Track Track { get; set; } = null!;
        public virtual Course Course { get; set; } = null!;
    }

    // ============================================================================
    // 2c. COHORTS (recurring batches a course runs as)
    // ============================================================================

    public class Cohort
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public string Name { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime EnrollmentCutoffDate { get; set; }
        public int Capacity { get; set; }
        public int GracePeriodDays { get; set; } = 14;
        public string Status { get; set; } = "draft"; // draft, open, cancelled, completed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Course Course { get; set; } = null!;
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<EnrollmentRequestCohort> RequestTargets { get; set; } = new List<EnrollmentRequestCohort>();
    }

    public class CourseInstructor
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public Guid InstructorId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Course Course { get; set; } = null!;
        public virtual User Instructor { get; set; } = null!;
    }

    public class Module
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int OrderIndex { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual Course Course { get; set; } = null!;
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
        public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }

    /// <summary>
    /// Unifies what were previously separate Lecture (pre-recorded, under a section)
    /// and LiveSession (flat per-course) entities into one type-discriminated model —
    /// a module's content is an ordered mix of live, in-person, and pre-recorded
    /// sessions (see docs/DATABASE.md §6).
    /// </summary>
    public class Session
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ModuleId { get; set; }
        public string Type { get; set; } = null!; // live, in_person, recorded_lesson
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int OrderIndex { get; set; }
        public DateTime? ScheduledAt { get; set; } // required for live/in_person
        public int? DurationMinutes { get; set; }
        public string? JoinLink { get; set; } // live only — external Zoom/Meet/Teams URL
        public string? Location { get; set; } // in_person only
        public string? VideoUrl { get; set; } // recorded_lesson content, or the post-session recording once available
        public Guid? InstructorId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Module Module { get; set; } = null!;
        public virtual User? Instructor { get; set; }
        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
        public virtual ICollection<SessionProgress> Progresses { get; set; } = new List<SessionProgress>();
        public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }

    public class Material
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ModuleId { get; set; }
        public Guid? SessionId { get; set; }
        public string Type { get; set; } = null!; // file, text, link
        public string Title { get; set; } = null!;
        public int OrderIndex { get; set; }
        public string? Body { get; set; } // text type only
        public string? FileUrl { get; set; } // file type only
        public string? FileType { get; set; } // file type only: pdf, ppt, zip, other
        public int? FileSizeKb { get; set; } // file type only
        public string? LinkUrl { get; set; } // link type only
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Module? Module { get; set; }
        public virtual Session? Session { get; set; }
    }

    // ============================================================================
    // 3. ENROLLMENT FLOW DOMAIN
    // ============================================================================

    public class EnrollmentRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ApplicantName { get; set; } = null!;
        public string ApplicantEmail { get; set; } = null!;
        public string? ApplicantPhone { get; set; }
        public Guid? CourseId { get; set; } // set when enrolling in a single course
        public Guid? TrackId { get; set; } // set when enrolling in a track bundle — exactly one of Course/Track is set
        public string PaymentMethod { get; set; } = null!;
        public string PaymentProofUrl { get; set; } = null!;
        public decimal OriginalPrice { get; set; }
        public string? CouponCode { get; set; }
        public Guid? CouponId { get; set; }
        public decimal DiscountAmount { get; set; } = 0.00m;
        public decimal FinalPrice { get; set; }
        public string Status { get; set; } = "pending"; // pending, approved, rejected
        public string? RejectionReason { get; set; }
        public Guid? ReviewedById { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Course? Course { get; set; }
        public virtual Track? Track { get; set; }
        public virtual Coupon? Coupon { get; set; }
        public virtual User? ReviewedBy { get; set; }
        public virtual ICollection<EnrollmentRequestCohort> TargetCohorts { get; set; } = new List<EnrollmentRequestCohort>();
        public virtual ICollection<Enrollment> ResultingEnrollments { get; set; } = new List<Enrollment>();
    }

    public class EnrollmentRequestCohort
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EnrollmentRequestId { get; set; }
        public Guid CohortId { get; set; }

        // Navigation properties
        public virtual EnrollmentRequest EnrollmentRequest { get; set; } = null!;
        public virtual Cohort Cohort { get; set; } = null!;
    }

    public class Coupon
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = null!; // stored normalized (uppercase)
        public string Type { get; set; } = null!; // percent, fixed
        public decimal Value { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; } = 0;
        public Guid CreatedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User CreatedBy { get; set; } = null!;
        public virtual ICollection<EnrollmentRequest> EnrollmentRequests { get; set; } = new List<EnrollmentRequest>();
    }

    public class Enrollment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public Guid CohortId { get; set; }
        public Guid? SourceRequestId { get; set; }
        public string Status { get; set; } = "active"; // active, expired, cancelled, refunded
        public DateTime? AccessExpiresAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public Guid? CancelledById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User Student { get; set; } = null!;
        public virtual Course Course { get; set; } = null!;
        public virtual Cohort Cohort { get; set; } = null!;
        public virtual EnrollmentRequest? SourceRequest { get; set; }
        public virtual User? CancelledBy { get; set; }
    }

    // ============================================================================
    // 4. LEARNING & PROGRESS DOMAIN
    // ============================================================================

    public class SessionProgress
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        public Guid SessionId { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User Student { get; set; } = null!;
        public virtual Session Session { get; set; } = null!;
    }

    /// <summary>
    /// Shared table for both quizzes and exams (Type = "quiz"/"exam", see
    /// AssessmentTypes) — both are MCQ-based, timed, pass-score assessments; exams add
    /// stricter controls (MaxAttempts forced to 1, RandomizeQuestions,
    /// DisableCopyPaste). Mirrors how Session merges live/in_person/recorded_lesson
    /// into one type-discriminated table (see docs/DATABASE.md §6, §7).
    /// </summary>
    public class Quiz
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ModuleId { get; set; }
        public string Type { get; set; } = null!; // quiz, exam
        public string Title { get; set; } = null!;
        public int OrderIndex { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public int? PassScore { get; set; } // percentage 0-100, enforced by chk_quiz_pass_score
        public bool IsPractice { get; set; } = false;
        public int? MaxAttempts { get; set; } // null = unlimited; exams are validated to 1
        public bool RandomizeQuestions { get; set; } = false;
        public bool DisableCopyPaste { get; set; } = false; // frontend UX deterrent only — no proctoring
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Module Module { get; set; } = null!;
        public virtual ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
        public virtual ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
    }

    public class QuizQuestion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid QuizId { get; set; }
        public string QuestionText { get; set; } = null!;
        public int OrderIndex { get; set; }

        // Navigation properties
        public virtual Quiz Quiz { get; set; } = null!;
        public virtual ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
        public virtual ICollection<QuizAnswer> QuizAnswers { get; set; } = new List<QuizAnswer>();
    }

    public class QuizOption
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid QuestionId { get; set; }
        public string OptionText { get; set; } = null!;
        public bool IsCorrect { get; set; } = false;
        public int OrderIndex { get; set; }

        // Navigation properties
        public virtual QuizQuestion Question { get; set; } = null!;
        public virtual ICollection<QuizAnswer> QuizAnswers { get; set; } = new List<QuizAnswer>();
    }

    public class QuizAttempt
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid QuizId { get; set; }
        public Guid StudentId { get; set; }
        public int AttemptNumber { get; set; } = 1;
        public int? Score { get; set; }
        public bool? Passed { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }

        // Navigation properties
        public virtual Quiz Quiz { get; set; } = null!;
        public virtual User Student { get; set; } = null!;
        public virtual ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
    }

    public class QuizAnswer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AttemptId { get; set; }
        public Guid QuestionId { get; set; }
        public Guid? SelectedOptionId { get; set; }

        // Navigation properties
        public virtual QuizAttempt Attempt { get; set; } = null!;
        public virtual QuizQuestion Question { get; set; } = null!;
        public virtual QuizOption? SelectedOption { get; set; }
    }

    // ============================================================================
    // 4b. ATTENDANCE
    // ============================================================================

    public class AttendanceRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SessionId { get; set; }
        public Guid StudentId { get; set; }
        public string Status { get; set; } = null!; // present, absent, late, excused
        public Guid MarkedById { get; set; }
        public DateTime MarkedAt { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Session Session { get; set; } = null!;
        public virtual User Student { get; set; } = null!;
        public virtual User MarkedBy { get; set; } = null!;
    }

    // ============================================================================
    // 4c. ASSIGNMENTS (code, Python auto-grader via ICodeExecutionService)
    // ============================================================================

    public class Assignment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ModuleId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!; // instructions
        public int OrderIndex { get; set; }
        public bool IsPractice { get; set; } = false;
        public int? MaxAttempts { get; set; } // null = unlimited
        public DateTime? DueAt { get; set; } // soft deadline — late allowed, never blocked
        public int? PassScore { get; set; } // percentage 0-100
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Module Module { get; set; } = null!;
        public virtual ICollection<AssignmentTestCase> TestCases { get; set; } = new List<AssignmentTestCase>();
        public virtual ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
    }

    public class AssignmentTestCase
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AssignmentId { get; set; }
        public string Input { get; set; } = null!; // stdin fed to the student's program
        public string ExpectedOutput { get; set; } = null!;
        public bool IsHidden { get; set; } = false; // hidden cases count toward score but aren't shown as examples
        public int Points { get; set; } = 1;
        public int OrderIndex { get; set; }

        // Navigation properties
        public virtual Assignment Assignment { get; set; } = null!;
        public virtual ICollection<AssignmentTestResult> Results { get; set; } = new List<AssignmentTestResult>();
    }

    public class AssignmentSubmission
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AssignmentId { get; set; }
        public Guid StudentId { get; set; }
        public string Code { get; set; } = null!;
        public int AttemptNumber { get; set; } = 1;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public bool IsLate { get; set; } = false; // computed at submission time vs Assignment.DueAt
        public int? AutoScore { get; set; }
        public string AutoGradingStatus { get; set; } = "pending"; // pending, completed, failed
        public int? ManualScore { get; set; }
        public string? ManualFeedback { get; set; }
        public int? FinalScore { get; set; } // ManualScore ?? AutoScore
        public Guid? GradedById { get; set; }
        public DateTime? GradedAt { get; set; }

        // Navigation properties
        public virtual Assignment Assignment { get; set; } = null!;
        public virtual User Student { get; set; } = null!;
        public virtual User? GradedBy { get; set; }
        public virtual ICollection<AssignmentTestResult> TestResults { get; set; } = new List<AssignmentTestResult>();
    }

    public class AssignmentTestResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SubmissionId { get; set; }
        public Guid TestCaseId { get; set; }
        public bool Passed { get; set; }
        public string? ActualOutput { get; set; }
        public string? ErrorMessage { get; set; }
        public int? ExecutionTimeMs { get; set; }

        // Navigation properties
        public virtual AssignmentSubmission Submission { get; set; } = null!;
        public virtual AssignmentTestCase TestCase { get; set; } = null!;
    }

    // ============================================================================
    // 5. COMMUNICATION DOMAIN
    // ============================================================================

    public class Announcement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? CourseId { get; set; } // Null = platform-wide
        public Guid AuthorId { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Course? Course { get; set; }
        public virtual User Author { get; set; } = null!;
    }

    public class Lead
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Message { get; set; }
        public Guid? CourseId { get; set; } // set when submitted from an "await next batch" context
        public bool IsContacted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Course? Course { get; set; }
    }

    // ============================================================================
    // 6. SYSTEM OBSERVABILITY
    // ============================================================================

    public class ActivityLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Action { get; set; } = null!;
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public JsonDocument? Metadata { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}
