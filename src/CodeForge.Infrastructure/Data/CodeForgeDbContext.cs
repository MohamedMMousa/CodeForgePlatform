using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;

namespace CodeForge.Infrastructure.Data
{
    public class CodeForgeDbContext : DbContext, ICodeForgeDbContext
    {
        public CodeForgeDbContext(DbContextOptions<CodeForgeDbContext> options)
            : base(options)
        {
        }

        // DbSets implementation
        public DbSet<User> Users => Set<User>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<CourseInstructor> CourseInstructors => Set<CourseInstructor>();
        public DbSet<Module> Modules => Set<Module>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<Material> Materials => Set<Material>();
        public DbSet<Track> Tracks => Set<Track>();
        public DbSet<TrackCourse> TrackCourses => Set<TrackCourse>();
        public DbSet<Cohort> Cohorts => Set<Cohort>();
        public DbSet<Coupon> Coupons => Set<Coupon>();
        public DbSet<EnrollmentRequest> EnrollmentRequests => Set<EnrollmentRequest>();
        public DbSet<EnrollmentRequestCohort> EnrollmentRequestCohorts => Set<EnrollmentRequestCohort>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<SessionProgress> SessionProgresses => Set<SessionProgress>();
        public DbSet<Quiz> Quizzes => Set<Quiz>();
        public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
        public DbSet<QuizOption> QuizOptions => Set<QuizOption>();
        public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
        public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
        public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
        public DbSet<Assignment> Assignments => Set<Assignment>();
        public DbSet<AssignmentTestCase> AssignmentTestCases => Set<AssignmentTestCase>();
        public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
        public DbSet<AssignmentTestResult> AssignmentTestResults => Set<AssignmentTestResult>();
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<Lead> Leads => Set<Lead>();
        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Auto-update timestamps before saving
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is User ||
                    entry.Entity is Course ||
                    entry.Entity is Module ||
                    entry.Entity is Session ||
                    entry.Entity is Material ||
                    entry.Entity is EnrollmentRequest ||
                    entry.Entity is Enrollment ||
                    entry.Entity is Quiz ||
                    entry.Entity is Announcement ||
                    entry.Entity is Track ||
                    entry.Entity is Cohort ||
                    entry.Entity is Coupon ||
                    entry.Entity is AttendanceRecord ||
                    entry.Entity is Assignment)
                {
                    if (entry.State == EntityState.Modified)
                    {
                        entry.Property("UpdatedAt").CurrentValue = System.DateTime.UtcNow;
                    }
                }
            }
            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================================================
            // 1. IDENTITY & AUTH DOMAIN
            // ============================================================================

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
                entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
                entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(20).IsRequired();
                entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                entity.Property(e => e.MustChangePassword).HasColumnName("must_change_password").HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");
                entity.Property(e => e.RefreshToken).HasColumnName("refresh_token").HasMaxLength(255);
                entity.Property(e => e.RefreshTokenExpiryTime).HasColumnName("refresh_token_expires_at").HasColumnType("timestamp with time zone");

                // Unique index
                entity.HasIndex(e => e.Email).IsUnique().HasFilter("deleted_at IS NULL");
                entity.HasIndex(e => e.Role);

                // Soft Delete Query Filter
                entity.HasQueryFilter(e => e.DeletedAt == null);
            });

            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.ToTable("password_reset_tokens");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.Token).HasColumnName("token").HasMaxLength(255).IsRequired();
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
                entity.Property(e => e.UsedAt).HasColumnName("used_at").HasColumnType("timestamp with time zone");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                entity.HasIndex(e => e.Token).IsUnique();

                // Relationship
                entity.HasOne(d => d.User)
                    .WithMany(p => p.PasswordResetTokens)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================================
            // 2. COURSE STRUCTURE DOMAIN
            // ============================================================================

            modelBuilder.Entity<Course>(entity =>
            {
                entity.ToTable("courses");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
                entity.Property(e => e.ThumbnailUrl).HasColumnName("thumbnail_url").HasMaxLength(500);
                entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(100);
                entity.Property(e => e.Price).HasColumnName("price").HasColumnType("numeric(10,2)").HasDefaultValue(0.00m);
                entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10).HasDefaultValue("EGP");
                entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("draft");
                entity.Property(e => e.CreatedById).HasColumnName("created_by").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");

                // Indexes
                entity.HasIndex(e => e.Slug).IsUnique().HasFilter("deleted_at IS NULL");
                entity.HasIndex(e => e.Status).HasFilter("deleted_at IS NULL");

                // Relationship
                entity.HasOne(d => d.CreatedBy)
                    .WithMany(p => p.CreatedCourses)
                    .HasForeignKey(d => d.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                // Soft Delete Query Filter
                entity.HasQueryFilter(e => e.DeletedAt == null);
            });

            // ============================================================================
            // 2b. TRACKS (course bundles)
            // ============================================================================

            modelBuilder.Entity<Track>(entity =>
            {
                entity.ToTable("tracks");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
                entity.Property(e => e.ThumbnailUrl).HasColumnName("thumbnail_url").HasMaxLength(500);
                entity.Property(e => e.Price).HasColumnName("price").HasColumnType("numeric(10,2)").HasDefaultValue(0.00m);
                entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10).HasDefaultValue("EGP");
                entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("draft");
                entity.Property(e => e.CreatedById).HasColumnName("created_by").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");

                entity.HasIndex(e => e.Slug).IsUnique().HasFilter("deleted_at IS NULL");
                entity.HasIndex(e => e.Status).HasFilter("deleted_at IS NULL");

                entity.HasOne(d => d.CreatedBy)
                    .WithMany(p => p.CreatedTracks)
                    .HasForeignKey(d => d.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(e => e.DeletedAt == null);
            });

            modelBuilder.Entity<TrackCourse>(entity =>
            {
                entity.ToTable("track_courses");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.TrackId).HasColumnName("track_id").IsRequired();
                entity.Property(e => e.CourseId).HasColumnName("course_id").IsRequired();
                entity.Property(e => e.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                entity.HasIndex(e => new { e.TrackId, e.CourseId }).IsUnique();

                entity.HasOne(d => d.Track)
                    .WithMany(p => p.TrackCourses)
                    .HasForeignKey(d => d.TrackId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Course)
                    .WithMany(p => p.TrackCourses)
                    .HasForeignKey(d => d.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================================
            // 2c. COHORTS (recurring batches)
            // ============================================================================

            modelBuilder.Entity<Cohort>(entity =>
            {
                entity.ToTable("cohorts");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.CourseId).HasColumnName("course_id").IsRequired();
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
                entity.Property(e => e.StartDate).HasColumnName("start_date").HasColumnType("timestamp with time zone").IsRequired();
                entity.Property(e => e.EndDate).HasColumnName("end_date").HasColumnType("timestamp with time zone").IsRequired();
                entity.Property(e => e.EnrollmentCutoffDate).HasColumnName("enrollment_cutoff_date").HasColumnType("timestamp with time zone").IsRequired();
                entity.Property(e => e.Capacity).HasColumnName("capacity").IsRequired();
                entity.Property(e => e.GracePeriodDays).HasColumnName("grace_period_days").HasDefaultValue(14);
                entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("draft");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                entity.HasIndex(e => e.CourseId);
                entity.HasIndex(e => e.Status);

                entity.HasOne(d => d.Course)
                    .WithMany(p => p.Cohorts)
                    .HasForeignKey(d => d.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Coupon>(entity =>
            {
                entity.ToTable("coupons");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(20).IsRequired();
                entity.Property(e => e.Value).HasColumnName("value").HasColumnType("numeric(10,2)").IsRequired();
                entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                entity.Property(e => e.ValidFrom).HasColumnName("valid_from").HasColumnType("timestamp with time zone");
                entity.Property(e => e.ValidUntil).HasColumnName("valid_until").HasColumnType("timestamp with time zone");
                entity.Property(e => e.UsageLimit).HasColumnName("usage_limit");
                entity.Property(e => e.UsedCount).HasColumnName("used_count").HasDefaultValue(0);
                entity.Property(e => e.CreatedById).HasColumnName("created_by").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                entity.HasIndex(e => e.Code).IsUnique();

                entity.HasOne(d => d.CreatedBy)
                    .WithMany(p => p.CreatedCoupons)
                    .HasForeignKey(d => d.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CourseInstructor>(entity =>
            {
                entity.ToTable("course_instructors");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.CourseId).HasColumnName("course_id").IsRequired();
                entity.Property(e => e.InstructorId).HasColumnName("instructor_id").IsRequired();
                entity.Property(e => e.AssignedAt).HasColumnName("assigned_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                entity.HasIndex(e => new { e.CourseId, e.InstructorId }).IsUnique();

                // Relationships
                entity.HasOne(d => d.Course)
                    .WithMany(p => p.Instructors)
                    .HasForeignKey(d => d.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Instructor)
                    .WithMany(p => p.CourseAssignments)
                    .HasForeignKey(d => d.InstructorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Module>(entity =>
            {
                entity.ToTable("modules");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.CourseId).HasColumnName("course_id").IsRequired();
                entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
                entity.Property(e => e.OrderIndex).HasColumnName("order_index").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");

                entity.HasIndex(e => e.CourseId);

                // Soft Delete Query Filter
                entity.HasQueryFilter(e => e.DeletedAt == null);

                entity.HasOne(d => d.Course)
                    .WithMany(p => p.Modules)
                    .HasForeignKey(d => d.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Session>(entity =>
            {
                entity.ToTable("sessions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.ModuleId).HasColumnName("module_id").IsRequired();
                entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(20).IsRequired();
                entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
                entity.Property(e => e.OrderIndex).HasColumnName("order_index").IsRequired();
                entity.Property(e => e.ScheduledAt).HasColumnName("scheduled_at").HasColumnType("timestamp with time zone");
                entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
                entity.Property(e => e.JoinLink).HasColumnName("join_link").HasMaxLength(500);
                entity.Property(e => e.Location).HasColumnName("location").HasMaxLength(255);
                entity.Property(e => e.VideoUrl).HasColumnName("video_url").HasMaxLength(500);
                entity.Property(e => e.InstructorId).HasColumnName("instructor_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                entity.HasIndex(e => e.ModuleId);
                entity.HasIndex(e => e.ScheduledAt);

                entity.HasOne(d => d.Module)
                    .WithMany(p => p.Sessions)
                    .HasForeignKey(d => d.ModuleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Instructor)
                    .WithMany(p => p.InstructedSessions)
                    .HasForeignKey(d => d.InstructorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Material>(entity =>
            {
                entity.ToTable("materials");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.ModuleId).HasColumnName("module_id");
                entity.Property(e => e.SessionId).HasColumnName("session_id");
                entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(20).IsRequired();
                entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
                entity.Property(e => e.OrderIndex).HasColumnName("order_index").IsRequired();
                entity.Property(e => e.Body).HasColumnName("body").HasColumnType("text");
                entity.Property(e => e.FileUrl).HasColumnName("file_url").HasMaxLength(500);
                entity.Property(e => e.FileType).HasColumnName("file_type").HasMaxLength(20);
                entity.Property(e => e.FileSizeKb).HasColumnName("file_size_kb");
                entity.Property(e => e.LinkUrl).HasColumnName("link_url").HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                entity.ToTable(t => t.HasCheckConstraint(
                    "chk_material_target",
                    "module_id IS NOT NULL OR session_id IS NOT NULL"));

                entity.HasOne(d => d.Module)
                    .WithMany(p => p.Materials)
                    .HasForeignKey(d => d.ModuleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Session)
                    .WithMany(p => p.Materials)
                    .HasForeignKey(d => d.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================================
            // 3. ENROLLMENT FLOW DOMAIN
            // ============================================================================

            modelBuilder.Entity<EnrollmentRequest>(entity =>
            {
                entity.ToTable("enrollment_requests");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.ApplicantName).HasColumnName("applicant_name").HasMaxLength(255).IsRequired();
                entity.Property(e => e.ApplicantEmail).HasColumnName("applicant_email").HasMaxLength(255).IsRequired();
                entity.Property(e => e.ApplicantPhone).HasColumnName("applicant_phone").HasMaxLength(20);
                entity.Property(e => e.CourseId).HasColumnName("course_id");
                entity.Property(e => e.TrackId).HasColumnName("track_id");
                entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasMaxLength(100).IsRequired();
                entity.Property(e => e.PaymentProofUrl).HasColumnName("payment_proof_url").HasMaxLength(500).IsRequired();
                entity.Property(e => e.OriginalPrice).HasColumnName("original_price").HasColumnType("numeric(10,2)").IsRequired();
                entity.Property(e => e.CouponCode).HasColumnName("coupon_code").HasMaxLength(50);
                entity.Property(e => e.CouponId).HasColumnName("coupon_id");
                entity.Property(e => e.DiscountAmount).HasColumnName("discount_amount").HasColumnType("numeric(10,2)").HasDefaultValue(0.00m);
                entity.Property(e => e.FinalPrice).HasColumnName("final_price").HasColumnType("numeric(10,2)").IsRequired();
                entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason").HasColumnType("text");
                entity.Property(e => e.ReviewedById).HasColumnName("reviewed_by");
                entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("timestamp with time zone");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ApplicantEmail);
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_enrollment_requests_course_xor_track",
                    "(course_id IS NOT NULL AND track_id IS NULL) OR (course_id IS NULL AND track_id IS NOT NULL)"));

                entity.HasOne(d => d.Course)
                    .WithMany(p => p.EnrollmentRequests)
                    .HasForeignKey(d => d.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Track)
                    .WithMany(p => p.EnrollmentRequests)
                    .HasForeignKey(d => d.TrackId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Coupon)
                    .WithMany(p => p.EnrollmentRequests)
                    .HasForeignKey(d => d.CouponId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.ReviewedBy)
                    .WithMany(p => p.ReviewedEnrollmentRequests)
                    .HasForeignKey(d => d.ReviewedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EnrollmentRequestCohort>(entity =>
            {
                entity.ToTable("enrollment_request_cohorts");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.EnrollmentRequestId).HasColumnName("enrollment_request_id").IsRequired();
                entity.Property(e => e.CohortId).HasColumnName("cohort_id").IsRequired();

                entity.HasIndex(e => new { e.EnrollmentRequestId, e.CohortId }).IsUnique();

                entity.HasOne(d => d.EnrollmentRequest)
                    .WithMany(p => p.TargetCohorts)
                    .HasForeignKey(d => d.EnrollmentRequestId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Cohort)
                    .WithMany(p => p.RequestTargets)
                    .HasForeignKey(d => d.CohortId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.ToTable("enrollments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.StudentId).HasColumnName("student_id").IsRequired();
                entity.Property(e => e.CourseId).HasColumnName("course_id").IsRequired();
                entity.Property(e => e.CohortId).HasColumnName("cohort_id").IsRequired();
                entity.Property(e => e.SourceRequestId).HasColumnName("source_request_id");
                entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("active");
                entity.Property(e => e.AccessExpiresAt).HasColumnName("access_expires_at").HasColumnType("timestamp with time zone");
                entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at").HasColumnType("timestamp with time zone");
                entity.Property(e => e.CancellationReason).HasColumnName("cancellation_reason").HasColumnType("text");
                entity.Property(e => e.CancelledById).HasColumnName("cancelled_by");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                // A student can re-enroll in a *later* cohort of the same course, so
                // uniqueness is per-cohort, not per-course — see docs/DATABASE.md §2.
                entity.HasIndex(e => new { e.StudentId, e.CohortId }).IsUnique();
                entity.HasIndex(e => e.Status);

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.Enrollments)
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Course)
                    .WithMany(p => p.Enrollments)
                    .HasForeignKey(d => d.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Cohort)
                    .WithMany(p => p.Enrollments)
                    .HasForeignKey(d => d.CohortId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.SourceRequest)
                    .WithMany(p => p.ResultingEnrollments)
                    .HasForeignKey(d => d.SourceRequestId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.CancelledBy)
                    .WithMany(p => p.CancelledEnrollments)
                    .HasForeignKey(d => d.CancelledById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================================
            // 4. LEARNING & PROGRESS DOMAIN
            // ============================================================================

            modelBuilder.Entity<SessionProgress>(entity =>
            {
                entity.ToTable("session_progress");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.StudentId).HasColumnName("student_id").IsRequired();
                entity.Property(e => e.SessionId).HasColumnName("session_id").IsRequired();
                entity.Property(e => e.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                entity.HasIndex(e => new { e.StudentId, e.SessionId }).IsUnique();

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.SessionProgresses)
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Session)
                    .WithMany(p => p.Progresses)
                    .HasForeignKey(d => d.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Quiz>(entity =>
            {
                entity.ToTable("quizzes");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.ModuleId).HasColumnName("module_id").IsRequired();
                entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(20).IsRequired();
                entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
                entity.Property(e => e.OrderIndex).HasColumnName("order_index").IsRequired();
                entity.Property(e => e.TimeLimitMinutes).HasColumnName("time_limit_minutes");
                entity.Property(e => e.PassScore).HasColumnName("pass_score");
                entity.Property(e => e.IsPractice).HasColumnName("is_practice").HasDefaultValue(false);
                entity.Property(e => e.MaxAttempts).HasColumnName("max_attempts");
                entity.Property(e => e.RandomizeQuestions).HasColumnName("randomize_questions").HasDefaultValue(false);
                entity.Property(e => e.DisableCopyPaste).HasColumnName("disable_copy_paste").HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                entity.HasIndex(e => e.ModuleId);
                entity.ToTable(t => t.HasCheckConstraint(
                    "chk_quiz_pass_score",
                    "pass_score IS NULL OR (pass_score BETWEEN 0 AND 100)"));

                entity.HasOne(d => d.Module)
                    .WithMany(p => p.Quizzes)
                    .HasForeignKey(d => d.ModuleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QuizQuestion>(entity =>
            {
                entity.ToTable("quiz_questions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.QuizId).HasColumnName("quiz_id").IsRequired();
                entity.Property(e => e.QuestionText).HasColumnName("question_text").HasColumnType("text").IsRequired();
                entity.Property(e => e.OrderIndex).HasColumnName("order_index").IsRequired();

                entity.HasIndex(e => e.QuizId);

                entity.HasOne(d => d.Quiz)
                    .WithMany(p => p.Questions)
                    .HasForeignKey(d => d.QuizId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QuizOption>(entity =>
            {
                entity.ToTable("quiz_options");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.QuestionId).HasColumnName("question_id").IsRequired();
                entity.Property(e => e.OptionText).HasColumnName("option_text").HasMaxLength(500).IsRequired();
                entity.Property(e => e.IsCorrect).HasColumnName("is_correct").HasDefaultValue(false);

                entity.HasIndex(e => e.QuestionId);

                entity.HasOne(d => d.Question)
                    .WithMany(p => p.Options)
                    .HasForeignKey(d => d.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QuizAttempt>(entity =>
            {
                entity.ToTable("quiz_attempts");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.QuizId).HasColumnName("quiz_id").IsRequired();
                entity.Property(e => e.StudentId).HasColumnName("student_id").IsRequired();
                entity.Property(e => e.AttemptNumber).HasColumnName("attempt_number").HasDefaultValue(1);
                entity.Property(e => e.Score).HasColumnName("score");
                entity.Property(e => e.Passed).HasColumnName("passed");
                entity.Property(e => e.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at").HasColumnType("timestamp with time zone");

                entity.HasIndex(e => e.StudentId);

                entity.HasOne(d => d.Quiz)
                    .WithMany(p => p.Attempts)
                    .HasForeignKey(d => d.QuizId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.QuizAttempts)
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<QuizAnswer>(entity =>
            {
                entity.ToTable("quiz_answers");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.AttemptId).HasColumnName("attempt_id").IsRequired();
                entity.Property(e => e.QuestionId).HasColumnName("question_id").IsRequired();
                entity.Property(e => e.SelectedOptionId).HasColumnName("selected_option_id");

                entity.HasOne(d => d.Attempt)
                    .WithMany(p => p.Answers)
                    .HasForeignKey(d => d.AttemptId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Question)
                    .WithMany(p => p.QuizAnswers)
                    .HasForeignKey(d => d.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.SelectedOption)
                    .WithMany(p => p.QuizAnswers)
                    .HasForeignKey(d => d.SelectedOptionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================================
            // 4b. ATTENDANCE
            // ============================================================================

            modelBuilder.Entity<AttendanceRecord>(entity =>
            {
                entity.ToTable("attendance_records");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.SessionId).HasColumnName("session_id").IsRequired();
                entity.Property(e => e.StudentId).HasColumnName("student_id").IsRequired();
                entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
                entity.Property(e => e.MarkedById).HasColumnName("marked_by").IsRequired();
                entity.Property(e => e.MarkedAt).HasColumnName("marked_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.Notes).HasColumnName("notes").HasColumnType("text");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                entity.HasIndex(e => new { e.SessionId, e.StudentId }).IsUnique();

                entity.HasOne(d => d.Session)
                    .WithMany(p => p.AttendanceRecords)
                    .HasForeignKey(d => d.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.AttendanceRecords)
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.MarkedBy)
                    .WithMany(p => p.MarkedAttendanceRecords)
                    .HasForeignKey(d => d.MarkedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================================
            // 4c. ASSIGNMENTS
            // ============================================================================

            modelBuilder.Entity<Assignment>(entity =>
            {
                entity.ToTable("assignments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.ModuleId).HasColumnName("module_id").IsRequired();
                entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text").IsRequired();
                entity.Property(e => e.OrderIndex).HasColumnName("order_index").IsRequired();
                entity.Property(e => e.IsPractice).HasColumnName("is_practice").HasDefaultValue(false);
                entity.Property(e => e.MaxAttempts).HasColumnName("max_attempts");
                entity.Property(e => e.DueAt).HasColumnName("due_at").HasColumnType("timestamp with time zone");
                entity.Property(e => e.PassScore).HasColumnName("pass_score");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                entity.HasIndex(e => e.ModuleId);
                entity.ToTable(t => t.HasCheckConstraint(
                    "chk_assignment_pass_score",
                    "pass_score IS NULL OR (pass_score BETWEEN 0 AND 100)"));

                entity.HasOne(d => d.Module)
                    .WithMany(p => p.Assignments)
                    .HasForeignKey(d => d.ModuleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AssignmentTestCase>(entity =>
            {
                entity.ToTable("assignment_test_cases");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.AssignmentId).HasColumnName("assignment_id").IsRequired();
                entity.Property(e => e.Input).HasColumnName("input").HasColumnType("text").IsRequired();
                entity.Property(e => e.ExpectedOutput).HasColumnName("expected_output").HasColumnType("text").IsRequired();
                entity.Property(e => e.IsHidden).HasColumnName("is_hidden").HasDefaultValue(false);
                entity.Property(e => e.Points).HasColumnName("points").HasDefaultValue(1);
                entity.Property(e => e.OrderIndex).HasColumnName("order_index").IsRequired();

                entity.HasIndex(e => e.AssignmentId);

                entity.HasOne(d => d.Assignment)
                    .WithMany(p => p.TestCases)
                    .HasForeignKey(d => d.AssignmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AssignmentSubmission>(entity =>
            {
                entity.ToTable("assignment_submissions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.AssignmentId).HasColumnName("assignment_id").IsRequired();
                entity.Property(e => e.StudentId).HasColumnName("student_id").IsRequired();
                entity.Property(e => e.Code).HasColumnName("code").HasColumnType("text").IsRequired();
                entity.Property(e => e.AttemptNumber).HasColumnName("attempt_number").HasDefaultValue(1);
                entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.IsLate).HasColumnName("is_late").HasDefaultValue(false);
                entity.Property(e => e.AutoScore).HasColumnName("auto_score");
                entity.Property(e => e.AutoGradingStatus).HasColumnName("auto_grading_status").HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.ManualScore).HasColumnName("manual_score");
                entity.Property(e => e.ManualFeedback).HasColumnName("manual_feedback").HasColumnType("text");
                entity.Property(e => e.FinalScore).HasColumnName("final_score");
                entity.Property(e => e.GradedById).HasColumnName("graded_by");
                entity.Property(e => e.GradedAt).HasColumnName("graded_at").HasColumnType("timestamp with time zone");

                entity.HasIndex(e => e.AssignmentId);
                entity.HasIndex(e => e.StudentId);

                entity.HasOne(d => d.Assignment)
                    .WithMany(p => p.Submissions)
                    .HasForeignKey(d => d.AssignmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.AssignmentSubmissions)
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.GradedBy)
                    .WithMany(p => p.GradedAssignmentSubmissions)
                    .HasForeignKey(d => d.GradedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AssignmentTestResult>(entity =>
            {
                entity.ToTable("assignment_test_results");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.SubmissionId).HasColumnName("submission_id").IsRequired();
                entity.Property(e => e.TestCaseId).HasColumnName("test_case_id").IsRequired();
                entity.Property(e => e.Passed).HasColumnName("passed").IsRequired();
                entity.Property(e => e.ActualOutput).HasColumnName("actual_output").HasColumnType("text");
                entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
                entity.Property(e => e.ExecutionTimeMs).HasColumnName("execution_time_ms");

                entity.HasIndex(e => e.SubmissionId);

                entity.HasOne(d => d.Submission)
                    .WithMany(p => p.TestResults)
                    .HasForeignKey(d => d.SubmissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.TestCase)
                    .WithMany(p => p.Results)
                    .HasForeignKey(d => d.TestCaseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================================
            // 5. COMMUNICATION DOMAIN
            // ============================================================================

            modelBuilder.Entity<Announcement>(entity =>
            {
                entity.ToTable("announcements");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.CourseId).HasColumnName("course_id");
                entity.Property(e => e.AuthorId).HasColumnName("author_id").IsRequired();
                entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Body).HasColumnName("body").HasColumnType("text").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                entity.HasOne(d => d.Course)
                    .WithMany(p => p.Announcements)
                    .HasForeignKey(d => d.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.AuthoredAnnouncements)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Lead>(entity =>
            {
                entity.ToTable("leads");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
                entity.Property(e => e.Message).HasColumnName("message").HasColumnType("text");
                entity.Property(e => e.CourseId).HasColumnName("course_id");
                entity.Property(e => e.IsContacted).HasColumnName("is_contacted").HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                entity.HasIndex(e => e.IsContacted);
                entity.HasIndex(e => e.CourseId);

                entity.HasOne(d => d.Course)
                    .WithMany()
                    .HasForeignKey(d => d.CourseId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ============================================================================
            // 6. SYSTEM OBSERVABILITY
            // ============================================================================

            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.ToTable("activity_logs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
                entity.Property(e => e.EntityType).HasColumnName("entity_type").HasMaxLength(100);
                entity.Property(e => e.EntityId).HasColumnName("entity_id");
                entity.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Action);
            });
        }
    }
}
