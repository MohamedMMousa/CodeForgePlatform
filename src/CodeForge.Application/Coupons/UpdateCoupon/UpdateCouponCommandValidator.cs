using CodeForge.Application.Common.Constants;
using FluentValidation;

namespace CodeForge.Application.Coupons.UpdateCoupon
{
    public class UpdateCouponCommandValidator : AbstractValidator<UpdateCouponCommand>
    {
        private static readonly string[] ValidTypes = { CouponTypes.Percent, CouponTypes.Fixed };

        public UpdateCouponCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();

            RuleFor(x => x.Type)
                .NotEmpty()
                .Must(type => ValidTypes.Contains(type))
                .WithMessage("Type must be 'percent' or 'fixed'.");

            RuleFor(x => x.Value)
                .GreaterThan(0);

            When(x => x.Type == CouponTypes.Percent, () =>
            {
                RuleFor(x => x.Value).LessThanOrEqualTo(100)
                    .WithMessage("A percent coupon cannot exceed 100.");
            });

            When(x => x.ValidFrom.HasValue && x.ValidUntil.HasValue, () =>
            {
                RuleFor(x => x.ValidUntil!.Value)
                    .GreaterThan(x => x.ValidFrom!.Value)
                    .WithMessage("Valid-until date must be after the valid-from date.");
            });

            RuleFor(x => x.UsageLimit)
                .GreaterThan(0)
                .When(x => x.UsageLimit.HasValue);
        }
    }
}
