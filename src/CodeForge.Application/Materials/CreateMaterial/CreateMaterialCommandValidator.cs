using CodeForge.Application.Common.Constants;
using CodeForge.Application.Materials.Common;
using FluentValidation;

namespace CodeForge.Application.Materials.CreateMaterial
{
    public class CreateMaterialCommandValidator : AbstractValidator<CreateMaterialCommand>
    {
        public CreateMaterialCommandValidator()
        {
            RuleFor(x => x)
                .Must(x => x.ModuleId.HasValue ^ x.SessionId.HasValue)
                .WithMessage("Specify exactly one of ModuleId or SessionId.")
                .WithName("ModuleId");

            RuleFor(x => x.Title).NotEmpty().MaximumLength(255);

            RuleFor(x => x.Type)
                .NotEmpty()
                .Must(type => MaterialValidationRules.ValidTypes.Contains(type))
                .WithMessage("Type must be 'file', 'text', or 'link'.");

            When(x => x.Type == MaterialTypes.Text, () =>
            {
                RuleFor(x => x.Body).NotEmpty().WithMessage("A text material requires body content.");
            });

            When(x => x.Type == MaterialTypes.Link, () =>
            {
                RuleFor(x => x.LinkUrl).NotEmpty().MaximumLength(500)
                    .WithMessage("A link material requires a URL.");
            });

            When(x => x.Type == MaterialTypes.File, () =>
            {
                RuleFor(x => x.FileStream).NotNull().WithMessage("A file material requires an uploaded file.");
                RuleFor(x => x.FileType)
                    .NotEmpty()
                    .Must(t => t == null || MaterialValidationRules.ValidFileTypes.Contains(t))
                    .WithMessage("FileType must be 'pdf', 'ppt', 'zip', or 'other'.");
            });
        }
    }
}
