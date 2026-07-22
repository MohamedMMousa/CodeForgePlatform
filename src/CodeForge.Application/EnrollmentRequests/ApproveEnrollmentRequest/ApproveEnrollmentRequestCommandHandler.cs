using System.Text.Json;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Notifications;
using CodeForge.Application.EnrollmentRequests.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.EnrollmentRequests.ApproveEnrollmentRequest
{
    public class ApproveEnrollmentRequestCommandHandler
        : IRequestHandler<ApproveEnrollmentRequestCommand, EnrollmentApprovalResultDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationDispatcher _notificationDispatcher;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITemporaryPasswordGenerator _temporaryPasswordGenerator;

        public ApproveEnrollmentRequestCommandHandler(
            ICodeForgeDbContext context,
            ICurrentUserService currentUserService,
            INotificationDispatcher notificationDispatcher,
            IPasswordHasher passwordHasher,
            ITemporaryPasswordGenerator temporaryPasswordGenerator)
        {
            _context = context;
            _currentUserService = currentUserService;
            _notificationDispatcher = notificationDispatcher;
            _passwordHasher = passwordHasher;
            _temporaryPasswordGenerator = temporaryPasswordGenerator;
        }

        public async Task<EnrollmentApprovalResultDto> Handle(
            ApproveEnrollmentRequestCommand request,
            CancellationToken cancellationToken)
        {
            var adminId = GetCurrentAdminId();
            var enrollmentRequest = await _context.EnrollmentRequests
                .Include(x => x.Course)
                .Include(x => x.Track)
                .Include(x => x.TargetCohorts).ThenInclude(tc => tc.Cohort).ThenInclude(c => c.Course)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (enrollmentRequest is null)
            {
                throw new KeyNotFoundException("Enrollment request was not found.");
            }

            if (enrollmentRequest.Status != EnrollmentRequestStatuses.Pending)
            {
                throw new InvalidOperationException("Only pending enrollment requests can be approved.");
            }

            var normalizedEmail = enrollmentRequest.ApplicantEmail.Trim().ToLower();
            var student = await _context.Users
                .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

            var studentCreated = false;
            string? temporaryPassword = null;

            if (student is null)
            {
                studentCreated = true;
                temporaryPassword = _temporaryPasswordGenerator.Generate();

                student = new User
                {
                    Email = normalizedEmail,
                    FullName = enrollmentRequest.ApplicantName,
                    Phone = enrollmentRequest.ApplicantPhone,
                    PasswordHash = _passwordHasher.HashPassword(temporaryPassword),
                    Role = Roles.Student,
                    IsActive = true,
                    MustChangePassword = true
                };

                _context.Users.Add(student);
            }
            else if (!student.IsActive)
            {
                throw new InvalidOperationException("A matching user exists but is inactive.");
            }

            var createdEnrollments = new List<(Enrollment Enrollment, string CourseTitle, DateTime AccessExpiresAt)>();

            foreach (var target in enrollmentRequest.TargetCohorts)
            {
                var cohort = target.Cohort;

                var alreadyEnrolled = await _context.Enrollments
                    .AnyAsync(x => x.StudentId == student.Id && x.CohortId == cohort.Id, cancellationToken);

                if (alreadyEnrolled)
                {
                    throw new InvalidOperationException(
                        $"Student is already enrolled in batch '{cohort.Name}'.");
                }

                var enrolledCount = await CohortAvailability.GetActiveEnrollmentCountAsync(
                    _context, cohort.Id, cancellationToken);

                if (enrolledCount >= cohort.Capacity)
                {
                    throw new InvalidOperationException(
                        $"Batch '{cohort.Name}' is now full and cannot accept this approval.");
                }

                var accessExpiresAt = cohort.EndDate.AddDays(cohort.GracePeriodDays);
                var enrollment = new Enrollment
                {
                    Student = student,
                    CourseId = cohort.CourseId,
                    CohortId = cohort.Id,
                    SourceRequestId = enrollmentRequest.Id,
                    Status = EnrollmentStatuses.Active,
                    AccessExpiresAt = accessExpiresAt
                };

                _context.Enrollments.Add(enrollment);
                createdEnrollments.Add((enrollment, cohort.Course.Title, accessExpiresAt));
            }

            enrollmentRequest.Status = EnrollmentRequestStatuses.Approved;
            enrollmentRequest.ReviewedById = adminId;
            enrollmentRequest.ReviewedAt = DateTime.UtcNow;
            enrollmentRequest.RejectionReason = null;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = adminId,
                Action = "enrollment_request.approved",
                EntityType = nameof(EnrollmentRequest),
                EntityId = enrollmentRequest.Id,
                Metadata = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    enrollmentRequestId = enrollmentRequest.Id,
                    courseId = enrollmentRequest.CourseId,
                    trackId = enrollmentRequest.TrackId,
                    studentEmail = normalizedEmail,
                    studentCreated,
                    courseTitles = createdEnrollments.Select(x => x.CourseTitle)
                }))
            });

            await _context.SaveChangesAsync(cancellationToken);

            foreach (var (_, courseTitle, accessExpiresAt) in createdEnrollments)
            {
                await _notificationDispatcher.DispatchAsync(
                    new NotificationEvent(
                        NotificationEventType.EnrollmentApproved,
                        student.Email,
                        student.FullName,
                        student.Phone,
                        new Dictionary<string, string>
                        {
                            ["courseTitle"] = courseTitle,
                            ["temporaryPassword"] = temporaryPassword ?? "",
                            ["accessExpiresAt"] = accessExpiresAt.ToString("d")
                        }),
                    cancellationToken);
            }

            return new EnrollmentApprovalResultDto(
                enrollmentRequest.Id,
                student.Id,
                createdEnrollments.Select(x => x.Enrollment.Id).ToList(),
                studentCreated,
                "Enrollment request approved.");
        }

        private Guid GetCurrentAdminId()
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var adminId))
            {
                throw new UnauthorizedAccessException("Authenticated admin could not be resolved.");
            }

            return adminId;
        }
    }
}
