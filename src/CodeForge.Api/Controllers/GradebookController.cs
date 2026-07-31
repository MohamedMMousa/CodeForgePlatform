using CodeForge.Application.Gradebook.GetCourseGradebook;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class GradebookController : ControllerBase
    {
        private readonly ISender _sender;

        public GradebookController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("courses/{courseId:guid}/gradebook")]
        [ProducesResponseType(typeof(CourseGradebookDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCourseGradebook(Guid courseId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetCourseGradebookQuery(courseId), cancellationToken);
            return Ok(response);
        }
    }
}
