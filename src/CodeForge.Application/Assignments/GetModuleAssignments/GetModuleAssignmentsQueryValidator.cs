using FluentValidation;

namespace CodeForge.Application.Assignments.GetModuleAssignments
{
    public class GetModuleAssignmentsQueryValidator : AbstractValidator<GetModuleAssignmentsQuery>
    {
        public GetModuleAssignmentsQueryValidator()
        {
            RuleFor(x => x.ModuleId).NotEmpty();
        }
    }
}
