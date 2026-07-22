using CodeForge.Application.Common.Constants;
using FluentValidation;

namespace CodeForge.Application.Certificates.IssueCertificate
{
    public class IssueCertificateCommandValidator : AbstractValidator<IssueCertificateCommand>
    {
        public IssueCertificateCommandValidator()
        {
            RuleFor(x => x.EnrollmentId).NotEmpty();

            RuleFor(x => x.Tier)
                .Must(tier => tier == CertificateTiers.Completion || tier == CertificateTiers.Participation)
                .When(x => x.Tier is not null)
                .WithMessage("Tier must be 'completion' or 'participation'.");
        }
    }
}
