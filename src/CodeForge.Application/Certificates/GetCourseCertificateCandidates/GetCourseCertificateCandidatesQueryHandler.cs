using CodeForge.Application.Certificates.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Certificates.GetCourseCertificateCandidates
{
    public class GetCourseCertificateCandidatesQueryHandler
        : IRequestHandler<GetCourseCertificateCandidatesQuery, CourseCertificateCandidatesDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetCourseCertificateCandidatesQueryHandler(
            ICodeForgeDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CourseCertificateCandidatesDto> Handle(
            GetCourseCertificateCandidatesQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var evaluation = await CourseEligibilityEvaluator.EvaluateAsync(_context, request.CourseId, cancellationToken);
            if (evaluation is null)
            {
                throw new KeyNotFoundException("Course was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, evaluation.Course, currentUserId);

            var existing = await _context.Certificates
                .AsNoTracking()
                .Include(c => c.Student)
                .Include(c => c.Course)
                .Include(c => c.Cohort)
                .Include(c => c.IssuedBy)
                .Where(c => c.CourseId == request.CourseId)
                .ToListAsync(cancellationToken);
            var byEnrollment = existing.ToDictionary(c => c.EnrollmentId, CertificateMapping.ToDto);

            var candidates = evaluation.Enrollments
                .OrderBy(e => e.Enrollment.Student.FullName)
                .Select(e => new CertificateCandidateDto(
                    e.Enrollment.Id,
                    e.Enrollment.StudentId,
                    e.Enrollment.Student.FullName,
                    e.Enrollment.Student.Email,
                    e.Enrollment.CohortId,
                    e.Enrollment.Cohort.Name,
                    e.AttendanceRate,
                    e.Result.AttendanceThreshold,
                    e.Result.AttendanceMet,
                    e.Result.AssessmentsPassed,
                    e.RequiredAssessmentCount,
                    e.Result.Tier,
                    byEnrollment.GetValueOrDefault(e.Enrollment.Id)))
                .ToList();

            return new CourseCertificateCandidatesDto(evaluation.Course.Id, evaluation.Course.Title, candidates);
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
