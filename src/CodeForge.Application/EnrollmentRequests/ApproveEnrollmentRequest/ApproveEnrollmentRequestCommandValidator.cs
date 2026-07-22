using FluentValidation;

namespace CodeForge.Application.EnrollmentRequests.ApproveEnrollmentRequest
{
    public class ApproveEnrollmentRequestCommandValidator : AbstractValidator<ApproveEnrollmentRequestCommand>
    {
        public ApproveEnrollmentRequestCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
