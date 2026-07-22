using FluentValidation;

namespace CodeForge.Application.Assessments.DeleteAssessment
{
    public class DeleteAssessmentCommandValidator : AbstractValidator<DeleteAssessmentCommand>
    {
        public DeleteAssessmentCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
