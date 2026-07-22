using FluentValidation;

namespace CodeForge.Application.Assignments.GetAssignmentById
{
    public class GetAssignmentByIdQueryValidator : AbstractValidator<GetAssignmentByIdQuery>
    {
        public GetAssignmentByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
