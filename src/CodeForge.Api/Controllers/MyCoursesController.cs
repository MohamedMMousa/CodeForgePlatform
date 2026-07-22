using CodeForge.Application.MyCourses.GetMyCourseContent;
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
        public async Task<IActionResult> GetUpcomingItems(CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetUpcomingItemsQuery(), cancellationToken);
            return Ok(response);
        }

        [HttpGet("{courseId:guid}/content")]
        public async Task<IActionResult> GetContent(Guid courseId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetMyCourseContentQuery(courseId), cancellationToken);
            return Ok(response);
        }
    }
}
