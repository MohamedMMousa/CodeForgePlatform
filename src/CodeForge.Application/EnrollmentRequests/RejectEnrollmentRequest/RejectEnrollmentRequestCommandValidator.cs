using FluentValidation;

namespace CodeForge.Application.EnrollmentRequests.RejectEnrollmentRequest
{
    public class RejectEnrollmentRequestCommandValidator : AbstractValidator<RejectEnrollmentRequestCommand>
    {
        public RejectEnrollmentRequestCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();

            RuleFor(x => x.RejectionReason)
                .NotEmpty()
                .MaximumLength(2000);
        }
    }
}
