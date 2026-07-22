using FluentValidation;

namespace CodeForge.Application.Assessments.DeleteQuestion
{
    public class DeleteQuestionCommandValidator : AbstractValidator<DeleteQuestionCommand>
    {
        public DeleteQuestionCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
