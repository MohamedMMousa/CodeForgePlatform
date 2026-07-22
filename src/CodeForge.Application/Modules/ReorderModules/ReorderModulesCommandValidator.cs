using FluentValidation;

namespace CodeForge.Application.Modules.ReorderModules
{
    public class ReorderModulesCommandValidator : AbstractValidator<ReorderModulesCommand>
    {
        public ReorderModulesCommandValidator()
        {
            RuleFor(x => x.CourseId).NotEmpty();
            RuleFor(x => x.ModuleOrders).NotEmpty();
        }
    }
}
