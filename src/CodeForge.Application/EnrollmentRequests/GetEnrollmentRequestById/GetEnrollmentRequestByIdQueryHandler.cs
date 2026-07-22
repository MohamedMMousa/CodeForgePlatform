using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.EnrollmentRequests.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.EnrollmentRequests.GetEnrollmentRequestById
{
    public class GetEnrollmentRequestByIdQueryHandler
        : IRequestHandler<GetEnrollmentRequestByIdQuery, EnrollmentRequestDetailDto>
    {
        private readonly ICodeForgeDbContext _context;

        public GetEnrollmentRequestByIdQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<EnrollmentRequestDetailDto> Handle(
            GetEnrollmentRequestByIdQuery request,
            CancellationToken cancellationToken)
        {
            var enrollmentRequest = await _context.EnrollmentRequests
                .AsNoTracking()
                .Include(x => x.Course)
                .Include(x => x.Track)
                .Include(x => x.ReviewedBy)
                .Include(x => x.TargetCohorts).ThenInclude(tc => tc.Cohort).ThenInclude(c => c.Course)
                .Include(x => x.ResultingEnrollments)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (enrollmentRequest is null)
            {
                throw new KeyNotFoundException("Enrollment request was not found.");
            }

            return new EnrollmentRequestDetailDto(
                enrollmentRequest.Id,
                enrollmentRequest.ApplicantName,
                enrollmentRequest.ApplicantEmail,
                enrollmentRequest.ApplicantPhone,
                enrollmentRequest.CourseId,
                enrollmentRequest.Course?.Title,
                enrollmentRequest.TrackId,
                enrollmentRequest.Track?.Title,
                enrollmentRequest.PaymentMethod,
                enrollmentRequest.PaymentProofUrl,
                enrollmentRequest.OriginalPrice,
                enrollmentRequest.CouponCode,
                enrollmentRequest.DiscountAmount,
                enrollmentRequest.FinalPrice,
                enrollmentRequest.Status,
                enrollmentRequest.RejectionReason,
                enrollmentRequest.ReviewedById,
                enrollmentRequest.ReviewedBy?.FullName,
                enrollmentRequest.ReviewedAt,
                enrollmentRequest.CreatedAt,
                enrollmentRequest.UpdatedAt,
                enrollmentRequest.TargetCohorts
                    .Select(tc => new EnrollmentRequestTargetCohortDto(
                        tc.CohortId,
                        tc.Cohort.Name,
                        tc.Cohort.CourseId,
                        tc.Cohort.Course.Title))
                    .ToList(),
                enrollmentRequest.ResultingEnrollments.Select(e => e.Id).ToList());
        }
    }
}
