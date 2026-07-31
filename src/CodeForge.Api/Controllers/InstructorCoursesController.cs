using CodeForge.Application.Courses.Common;
using CodeForge.Application.Courses.GetAssignedCourses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Route("instructor/courses")]
    [Authorize(Policy = "InstructorOnly")]
    [Produces("application/json")]
    public class InstructorCoursesController : ControllerBase
    {
        private readonly ISender _sender;

        public InstructorCoursesController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Get courses assigned to the current instructor.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<CourseListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAssignedCourses(CancellationToken cancellationToken)
        {
            // Exceptions are translated centrally by ExceptionHandlingMiddleware.
            var response = await _sender.Send(new GetAssignedCoursesQuery(), cancellationToken);
            return Ok(response);
        }
    }
}
