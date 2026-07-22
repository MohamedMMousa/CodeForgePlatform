using CodeForge.Application.Courses.GetPublishedCourseDetail;
using CodeForge.Application.Courses.GetPublishedCourses;
using CodeForge.Application.Tracks.GetPublishedTrackDetail;
using CodeForge.Application.Tracks.GetPublishedTracks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Route("catalog/courses")]
    [AllowAnonymous]
    [Produces("application/json")]
    public class CatalogController : ControllerBase
    {
        private readonly ISender _sender;

        public CatalogController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// List published courses visible in the public catalog.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublishedCourses(
            [FromQuery] string? category,
            [FromQuery] string? search,
            CancellationToken cancellationToken)
        {
            return await SendCatalogRequest(new GetPublishedCoursesQuery(category, search), cancellationToken);
        }

        /// <summary>
        /// Get published course details by slug, including open-batch availability.
        /// </summary>
        [HttpGet("{slug}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPublishedCourseDetail(
            string slug,
            CancellationToken cancellationToken)
        {
            return await SendCatalogRequest(new GetPublishedCourseDetailQuery(slug), cancellationToken);
        }

        /// <summary>
        /// List published tracks (course bundles) visible in the public catalog.
        /// </summary>
        [HttpGet("/catalog/tracks")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublishedTracks(
            [FromQuery] string? search,
            CancellationToken cancellationToken)
        {
            return await SendCatalogRequest(new GetPublishedTracksQuery(search), cancellationToken);
        }

        /// <summary>
        /// Get published track details by slug, including bundle-enrollment availability.
        /// </summary>
        [HttpGet("/catalog/tracks/{slug}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPublishedTrackDetail(
            string slug,
            CancellationToken cancellationToken)
        {
            return await SendCatalogRequest(new GetPublishedTrackDetailQuery(slug), cancellationToken);
        }

        private async Task<IActionResult> SendCatalogRequest<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken)
        {
            // Exceptions are translated centrally by ExceptionHandlingMiddleware.
            var response = await _sender.Send(request, cancellationToken);
            return Ok(response);
        }
    }
}
