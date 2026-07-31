using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Tracks.AddCourseToTrack;
using CodeForge.Application.Tracks.ArchiveTrack;
using CodeForge.Application.Tracks.Common;
using CodeForge.Application.Tracks.CreateTrack;
using CodeForge.Application.Tracks.DeleteTrack;
using CodeForge.Application.Tracks.GetTrackById;
using CodeForge.Application.Tracks.GetTracks;
using CodeForge.Application.Tracks.PublishTrack;
using CodeForge.Application.Tracks.RemoveCourseFromTrack;
using CodeForge.Application.Tracks.UpdateTrack;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Route("tracks")]
    [Authorize(Policy = "AdminOnly")]
    [Produces("application/json")]
    public class TracksController : ControllerBase
    {
        private readonly ISender _sender;

        public TracksController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        [ProducesResponseType(typeof(TrackDetailDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create(CreateTrackRequest request, CancellationToken cancellationToken)
        {
            return await SendTrackRequest(
                new CreateTrackCommand(
                    request.Title, request.Slug, request.Description,
                    request.ThumbnailUrl, request.Price, request.Currency),
                cancellationToken);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(TrackDetailDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, UpdateTrackRequest request, CancellationToken cancellationToken)
        {
            return await SendTrackRequest(
                new UpdateTrackCommand(
                    id, request.Title, request.Slug, request.Description,
                    request.ThumbnailUrl, request.Price, request.Currency),
                cancellationToken);
        }

        [HttpPut("{id:guid}/publish")]
        [ProducesResponseType(typeof(TrackMutationResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
        {
            return await SendTrackRequest(new PublishTrackCommand(id), cancellationToken);
        }

        [HttpPut("{id:guid}/archive")]
        [ProducesResponseType(typeof(TrackMutationResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
        {
            return await SendTrackRequest(new ArchiveTrackCommand(id), cancellationToken);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(TrackMutationResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            return await SendTrackRequest(new DeleteTrackCommand(id), cancellationToken);
        }

        [HttpPost("{id:guid}/courses/{courseId:guid}")]
        [ProducesResponseType(typeof(TrackCourseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddCourse(
            Guid id, Guid courseId, AddCourseToTrackRequest request, CancellationToken cancellationToken)
        {
            return await SendTrackRequest(
                new AddCourseToTrackCommand(id, courseId, request.SortOrder),
                cancellationToken);
        }

        [HttpDelete("{id:guid}/courses/{courseId:guid}")]
        [ProducesResponseType(typeof(TrackMutationResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveCourse(Guid id, Guid courseId, CancellationToken cancellationToken)
        {
            return await SendTrackRequest(new RemoveCourseFromTrackCommand(id, courseId), cancellationToken);
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<TrackListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] int page = PaginationDefaults.Page,
            [FromQuery] int pageSize = PaginationDefaults.PageSize,
            CancellationToken cancellationToken = default)
        {
            return await SendTrackRequest(new GetTracksQuery(status, search, page, pageSize), cancellationToken);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(TrackDetailDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            return await SendTrackRequest(new GetTrackByIdQuery(id), cancellationToken);
        }

        private async Task<IActionResult> SendTrackRequest<TResponse>(
            IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(request, cancellationToken);
            return Ok(response);
        }

        public record CreateTrackRequest(
            string Title, string Slug, string? Description, string? ThumbnailUrl, decimal Price, string Currency);

        public record UpdateTrackRequest(
            string Title, string Slug, string? Description, string? ThumbnailUrl, decimal Price, string Currency);

        public record AddCourseToTrackRequest(int SortOrder);
    }
}
