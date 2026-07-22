using FluentValidation;

namespace CodeForge.Application.Coupons.ValidateCoupon
{
    public class ValidateCouponQueryValidator : AbstractValidator<ValidateCouponQuery>
    {
        public ValidateCouponQueryValidator()
        {
            RuleFor(x => x.Code).NotEmpty().MaximumLength(50);

            RuleFor(x => x)
                .Must(x => x.CourseId.HasValue ^ x.TrackId.HasValue)
                .WithMessage("Specify exactly one of CourseId or TrackId.")
                .WithName("CourseId");
        }
    }
}
