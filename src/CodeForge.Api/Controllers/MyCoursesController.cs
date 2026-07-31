using CodeForge.Application.MyCourses.Common;
using CodeForge.Application.MyCourses.GetMyCourseContent;
using CodeForge.Application.MyCourses.GetMyCourseGrades;
using CodeForge.Application.MyCourses.GetUpcomingItems;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Route("my-courses")]
    [Authorize]
    [Produces("application/json")]
    public class MyCoursesController : ControllerBase
    {
        private readonly ISender _sender;

        public MyCoursesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("upcoming-items")]
        [ProducesResponseType(typeof(UpcomingItemsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUpcomingItems(CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetUpcomingItemsQuery(), cancellationToken);
            return Ok(response);
        }

        [HttpGet("{courseId:guid}/content")]
        [ProducesResponseType(typeof(MyCourseContentDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetContent(Guid courseId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetMyCourseContentQuery(courseId), cancellationToken);
            return Ok(response);
        }

        [HttpGet("{courseId:guid}/grades")]
        [ProducesResponseType(typeof(MyCourseGradesDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGrades(Guid courseId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetMyCourseGradesQuery(courseId), cancellationToken);
            return Ok(response);
        }
    }
}
