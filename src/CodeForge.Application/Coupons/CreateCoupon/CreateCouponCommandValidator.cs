using CodeForge.Application.Common.Constants;
using FluentValidation;

namespace CodeForge.Application.Coupons.CreateCoupon
{
    public class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
    {
        private static readonly string[] ValidTypes = { CouponTypes.Percent, CouponTypes.Fixed };

        public CreateCouponCommandValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .MaximumLength(50)
                .Matches("^[A-Za-z0-9_-]+$")
                .WithMessage("Coupon code may only contain letters, numbers, hyphens, and underscores.");

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
