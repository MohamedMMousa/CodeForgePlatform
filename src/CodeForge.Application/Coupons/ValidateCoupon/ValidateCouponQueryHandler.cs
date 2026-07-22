using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Coupons.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Coupons.ValidateCoupon
{
    /// <summary>
    /// Preview-only: does not increment coupon usage. Actual application (and usage
    /// accounting) happens at enrollment submission — see docs/DATABASE.md §4.
    /// </summary>
    public class ValidateCouponQueryHandler : IRequestHandler<ValidateCouponQuery, CouponValidationResultDto>
    {
        private readonly ICodeForgeDbContext _context;

        public ValidateCouponQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<CouponValidationResultDto> Handle(
            ValidateCouponQuery request,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var code = request.Code.Trim().ToUpperInvariant();

            decimal originalPrice;
            if (request.CourseId.HasValue)
            {
                var course = await _context.Courses
                    .FirstOrDefaultAsync(x => x.Id == request.CourseId.Value, cancellationToken);
                if (course is null || course.Status != CourseStatuses.Published)
                {
                    throw new KeyNotFoundException("Selected course was not found.");
                }
                originalPrice = course.Price;
            }
            else
            {
                var track = await _context.Tracks
                    .FirstOrDefaultAsync(x => x.Id == request.TrackId!.Value, cancellationToken);
                if (track is null || track.Status != TrackStatuses.Published)
                {
                    throw new KeyNotFoundException("Selected track was not found.");
                }
                originalPrice = track.Price;
            }

            var coupon = await _context.Coupons.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

            if (coupon is null || !CouponCalculator.IsValid(coupon, now))
            {
                return new CouponValidationResultDto(
                    false, code, null, null, originalPrice, 0m, originalPrice,
                    "This coupon code is not valid.");
            }

            var discount = CouponCalculator.CalculateDiscount(coupon, originalPrice);

            return new CouponValidationResultDto(
                true, code, coupon.Type, coupon.Value, originalPrice, discount, originalPrice - discount, null);
        }
    }
}
