using CodeForge.Application.Coupons.Common;
using MediatR;

namespace CodeForge.Application.Coupons.ValidateCoupon
{
    public record ValidateCouponQuery(
        string Code,
        Guid? CourseId,
        Guid? TrackId) : IRequest<CouponValidationResultDto>;
}
