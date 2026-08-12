using CodeForge.Application.Common.Validation;
using FluentValidation;

namespace CodeForge.Application.Cohorts.CreateCohort
{
    public class CreateCohortCommandValidator : AbstractValidator<CreateCohortCommand>
    {
        public CreateCohortCommandValidator()
        {
            RuleFor(x => x.CourseId).NotEmpty();

            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.StartDate).MustBeUtc();
            RuleFor(x => x.EndDate).MustBeUtc();
            RuleFor(x => x.EnrollmentCutoffDate).MustBeUtc();

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("End date must be after the start date.");

            RuleFor(x => x.EnrollmentCutoffDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("Enrollment cutoff must be on or before the end date.");

            RuleFor(x => x.Capacity)
                .GreaterThan(0);

            RuleFor(x => x.GracePeriodDays)
                .GreaterThanOrEqualTo(0);
        }
    }
}
