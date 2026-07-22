using FluentValidation;

namespace CodeForge.Application.EnrollmentRequests.GetEnrollmentRequestById
{
    public class GetEnrollmentRequestByIdQueryValidator : AbstractValidator<GetEnrollmentRequestByIdQuery>
    {
        public GetEnrollmentRequestByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
