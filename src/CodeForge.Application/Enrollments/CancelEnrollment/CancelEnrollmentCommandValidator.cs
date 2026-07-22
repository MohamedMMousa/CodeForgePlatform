using FluentValidation;

namespace CodeForge.Application.Enrollments.CancelEnrollment
{
    public class CancelEnrollmentCommandValidator : AbstractValidator<CancelEnrollmentCommand>
    {
        public CancelEnrollmentCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();

            RuleFor(x => x.Reason)
                .NotEmpty()
                .MaximumLength(1000);
        }
    }
}
