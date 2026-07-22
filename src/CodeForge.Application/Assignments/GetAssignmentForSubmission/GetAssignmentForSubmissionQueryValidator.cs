using FluentValidation;

namespace CodeForge.Application.Assignments.GetAssignmentForSubmission
{
    public class GetAssignmentForSubmissionQueryValidator : AbstractValidator<GetAssignmentForSubmissionQuery>
    {
        public GetAssignmentForSubmissionQueryValidator()
        {
            RuleFor(x => x.AssignmentId).NotEmpty();
        }
    }
}
