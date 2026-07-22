using FluentValidation;

namespace CodeForge.Application.Sessions.GetSessionById
{
    public class GetSessionByIdQueryValidator : AbstractValidator<GetSessionByIdQuery>
    {
        public GetSessionByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
