using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.EnrollmentRequests.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.EnrollmentRequests.SubmitEnrollmentRequest
{
    public class SubmitEnrollmentRequestCommandHandler
        : IRequestHandler<SubmitEnrollmentRequestCommand, EnrollmentRequestDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly IFileStorageService _fileStorageService;

        public SubmitEnrollmentRequestCommandHandler(
            ICodeForgeDbContext context,
            IFileStorageService fileStorageService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
        }

        public async Task<EnrollmentRequestDto> Handle(
            SubmitEnrollmentRequestCommand request,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            Course? course = null;
            Track? track = null;
            decimal originalPrice;
            var targetCohorts = new List<Cohort>();

            if (request.CourseId.HasValue)
            {
                course = await _context.Courses
                    .FirstOrDefaultAsync(x => x.Id == request.CourseId.Value, cancellationToken);

                if (course is null || course.Status != CourseStatuses.Published)
                {
                    throw new KeyNotFoundException("Selected course was not found.");
                }

                var cohort = await CohortAvailability.FindOpenCohortAsync(_context, course.Id, now, cancellationToken);
                if (cohort is null)
                {
                    throw new InvalidOperationException(
                        "No open batch is currently accepting enrollment for this course.");
                }

                originalPrice = course.Price;
                targetCohorts.Add(cohort);
            }
            else
            {
                track = await _context.Tracks
                    .Include(x => x.TrackCourses).ThenInclude(tc => tc.Course)
                    .FirstOrDefaultAsync(x => x.Id == request.TrackId!.Value, cancellationToken);

                if (track is null || track.Status != TrackStatuses.Published)
                {
                    throw new KeyNotFoundException("Selected track was not found.");
                }

                if (track.TrackCourses.Count == 0)
                {
                    throw new InvalidOperationException("This track has no courses yet.");
                }

                foreach (var trackCourse in track.TrackCourses)
                {
                    var cohort = await CohortAvailability.FindOpenCohortAsync(
                        _context, trackCourse.CourseId, now, cancellationToken);

                    if (cohort is null)
                    {
                        throw new InvalidOperationException(
                            "This track isn't fully available for bundle enrollment right now — " +
                            "try enrolling in its individually open courses instead.");
                    }

                    targetCohorts.Add(cohort);
                }

                originalPrice = track.Price;
            }

            Coupon? coupon = null;
            var discountAmount = 0.00m;
            string? normalizedCouponCode = null;

            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                normalizedCouponCode = request.CouponCode.Trim().ToUpperInvariant();
                coupon = await _context.Coupons
                    .FirstOrDefaultAsync(x => x.Code == normalizedCouponCode, cancellationToken);

                if (coupon is null || !CouponCalculator.IsValid(coupon, now))
                {
                    throw new InvalidOperationException("This coupon code is not valid.");
                }

                discountAmount = CouponCalculator.CalculateDiscount(coupon, originalPrice);
                coupon.UsedCount += 1;
            }

            var paymentProofUrl = await _fileStorageService.SavePaymentProofAsync(
                request.PaymentProofStream,
                request.PaymentProofFileName,
                request.PaymentProofContentType,
                cancellationToken);

            var enrollmentRequest = new EnrollmentRequest
            {
                ApplicantName = request.FullName.Trim(),
                ApplicantEmail = request.Email.Trim().ToLower(),
                ApplicantPhone = string.IsNullOrWhiteSpace(request.PhoneNumber)
                    ? null
                    : request.PhoneNumber.Trim(),
                CourseId = course?.Id,
                TrackId = track?.Id,
                PaymentMethod = request.PaymentMethod.Trim(),
                PaymentProofUrl = paymentProofUrl,
                OriginalPrice = originalPrice,
                CouponCode = normalizedCouponCode,
                CouponId = coupon?.Id,
                DiscountAmount = discountAmount,
                FinalPrice = originalPrice - discountAmount,
                Status = EnrollmentRequestStatuses.Pending
            };

            _context.EnrollmentRequests.Add(enrollmentRequest);

            foreach (var cohort in targetCohorts)
            {
                _context.EnrollmentRequestCohorts.Add(new EnrollmentRequestCohort
                {
                    EnrollmentRequest = enrollmentRequest,
                    CohortId = cohort.Id
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new EnrollmentRequestDto(
                enrollmentRequest.Id,
                enrollmentRequest.ApplicantName,
                enrollmentRequest.ApplicantEmail,
                enrollmentRequest.ApplicantPhone,
                course?.Id,
                course?.Title,
                track?.Id,
                track?.Title,
                enrollmentRequest.PaymentMethod,
                enrollmentRequest.PaymentProofUrl,
                enrollmentRequest.OriginalPrice,
                enrollmentRequest.CouponCode,
                enrollmentRequest.DiscountAmount,
                enrollmentRequest.FinalPrice,
                enrollmentRequest.Status,
                enrollmentRequest.CreatedAt,
                enrollmentRequest.UpdatedAt);
        }
    }
}
