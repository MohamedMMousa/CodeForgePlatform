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
        public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
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

    public class Quiz
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public string Title { get; set; } = null!;
        public int? TimeLimitMinutes { get; set; }
        public int? PassScore { get; set; }
        public bool AllowRetake { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Course Course { get; set; } = null!;
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

        // Navigation properties
        public virtual QuizQuestion Question { get; set; } = null!;
        public virtual ICollection<QuizAnswer> QuizAnswers { get; set; } = new List<QuizAnswer>();
    }

    public class QuizAttempt
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid QuizId { get; set; }
        public Guid StudentId { get; set; }
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
