using FluentValidation;

namespace CodeForge.Application.Tracks.GetTrackById
{
    public class GetTrackByIdQueryValidator : AbstractValidator<GetTrackByIdQuery>
    {
        public GetTrackByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
