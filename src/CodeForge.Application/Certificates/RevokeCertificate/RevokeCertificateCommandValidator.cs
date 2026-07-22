using FluentValidation;

namespace CodeForge.Application.Certificates.RevokeCertificate
{
    public class RevokeCertificateCommandValidator : AbstractValidator<RevokeCertificateCommand>
    {
        public RevokeCertificateCommandValidator()
        {
            RuleFor(x => x.CertificateId).NotEmpty();

            RuleFor(x => x.Reason)
                .MaximumLength(1000);
        }
    }
}
