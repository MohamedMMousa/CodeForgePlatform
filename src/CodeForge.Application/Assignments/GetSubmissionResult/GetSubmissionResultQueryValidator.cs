using FluentValidation;

namespace CodeForge.Application.Assignments.GetSubmissionResult
{
    public class GetSubmissionResultQueryValidator : AbstractValidator<GetSubmissionResultQuery>
    {
        public GetSubmissionResultQueryValidator()
        {
            RuleFor(x => x.SubmissionId).NotEmpty();
        }
    }
}
