using FluentValidation;

namespace CodeForge.Application.Coupons.DeactivateCoupon
{
    public class DeactivateCouponCommandValidator : AbstractValidator<DeactivateCouponCommand>
    {
        public DeactivateCouponCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
