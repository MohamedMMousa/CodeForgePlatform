using FluentValidation;

namespace CodeForge.Application.EnrollmentRequests.SubmitEnrollmentRequest
{
    public class SubmitEnrollmentRequestCommandValidator : AbstractValidator<SubmitEnrollmentRequestCommand>
    {
        private static readonly string[] AllowedContentTypes =
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "application/pdf"
        };

        public SubmitEnrollmentRequestCommandValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20);

            RuleFor(x => x)
                .Must(x => x.CourseId.HasValue ^ x.TrackId.HasValue)
                .WithMessage("Specify exactly one of CourseId or TrackId.")
                .WithName("CourseId");

            RuleFor(x => x.PaymentMethod)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.CouponCode)
                .MaximumLength(50);

            RuleFor(x => x.PaymentProofStream)
                .NotNull()
                .Must(stream => stream.CanRead)
                .WithMessage("Payment proof file is required.");

            RuleFor(x => x.PaymentProofFileName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.PaymentProofContentType)
                .NotEmpty()
                .Must(contentType => AllowedContentTypes.Contains(contentType))
                .WithMessage("Payment proof must be a JPG, PNG, WEBP, or PDF file.");
        }
    }
}
