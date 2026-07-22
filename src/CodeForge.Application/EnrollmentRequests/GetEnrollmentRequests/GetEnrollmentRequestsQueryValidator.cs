using CodeForge.Application.Common.Constants;
using FluentValidation;

namespace CodeForge.Application.EnrollmentRequests.GetEnrollmentRequests
{
    public class GetEnrollmentRequestsQueryValidator : AbstractValidator<GetEnrollmentRequestsQuery>
    {
        private static readonly string[] AllowedStatuses =
        {
            EnrollmentRequestStatuses.Pending,
            EnrollmentRequestStatuses.Approved,
            EnrollmentRequestStatuses.Rejected
        };

        public GetEnrollmentRequestsQueryValidator()
        {
            When(x => !string.IsNullOrWhiteSpace(x.Status), () =>
            {
                RuleFor(x => x.Status!)
                    .Must(status => AllowedStatuses.Contains(status.ToLower()))
                    .WithMessage("Status must be pending, approved, or rejected.");
            });
        }
    }
}
