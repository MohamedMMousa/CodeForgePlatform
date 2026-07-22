using CodeForge.Application.Tracks.Common;
using MediatR;

namespace CodeForge.Application.Tracks.GetPublishedTrackDetail
{
    public record GetPublishedTrackDetailQuery(string Slug) : IRequest<PublicTrackDetailDto>;
}
