using FluentValidation;

namespace CodeForge.Application.Cohorts.UpdateCohort
{
    public class UpdateCohortCommandValidator : AbstractValidator<UpdateCohortCommand>
    {
        public UpdateCohortCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();

            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(255);

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
