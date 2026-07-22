using FluentValidation;

namespace CodeForge.Application.Coupons.GetCouponById
{
    public class GetCouponByIdQueryValidator : AbstractValidator<GetCouponByIdQuery>
    {
        public GetCouponByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
