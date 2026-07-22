using CodeForge.Application.Coupons.Common;
using MediatR;

namespace CodeForge.Application.Coupons.GetCouponById
{
    public record GetCouponByIdQuery(Guid Id) : IRequest<CouponDto>;
}
