using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Courses.ArchiveCourse;
using CodeForge.Application.Courses.AssignInstructorToCourse;
using CodeForge.Application.Courses.Common;
using CodeForge.Application.Courses.CreateCourse;
using CodeForge.Application.Courses.DeleteCourse;
using CodeForge.Application.Courses.GetCourseById;
using CodeForge.Application.Courses.GetCourseInstructors;
using CodeForge.Application.Courses.GetCourses;
using CodeForge.Application.Courses.PublishCourse;
using CodeForge.Application.Courses.RemoveInstructorFromCourse;
using CodeForge.Application.Courses.UpdateCourse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Route("courses")]
    [Authorize(Policy = "AdminOnly")]
    [Produces("application/json")]
    public class CoursesController : ControllerBase
    {
        private readonly ISender _sender;

        public CoursesController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Create a draft course. Admin only.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create(CreateCourseRequest request, CancellationToken cancellationToken)
        {
            return await SendCourseRequest(
                new CreateCourseCommand(
                    request.Title,
                    request.Slug,
                    request.Description,
                    request.ThumbnailUrl,
                    request.Category,
                    request.Price,
                    request.Currency),
                cancellationToken);
        }

        /// <summary>
        /// Update course settings. Admin only.
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(
            Guid id,
            UpdateCourseRequest request,
            CancellationToken cancellationToken)
        {
            return await SendCourseRequest(
                new UpdateCourseCommand(
                    id,
                    request.Title,
                    request.Slug,
                    request.Description,
                    request.ThumbnailUrl,
                    request.Category,
                    request.Price,
                    request.Currency,
                    request.CompletionAttendanceThreshold),
                cancellationToken);
        }

        /// <summary>
        /// Publish a course so it appears in the catalog. Admin only.
        /// </summary>
        [HttpPut("{id:guid}/publish")]
        [ProducesResponseType(typeof(CourseMutationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
        {
            return await SendCourseRequest(new PublishCourseCommand(id), cancellationToken);
        }

        /// <summary>
        /// Archive a course and hide it from public catalog listings. Admin only.
        /// </summary>
        [HttpPut("{id:guid}/archive")]
        [ProducesResponseType(typeof(CourseMutationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
        {
            return await SendCourseRequest(new ArchiveCourseCommand(id), cancellationToken);
        }

        /// <summary>
        /// Soft delete a course. Admin only.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(CourseMutationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            return await SendCourseRequest(new DeleteCourseCommand(id), cancellationToken);
        }

        /// <summary>
        /// Assign an instructor to a course. Admin only.
        /// </summary>
        [HttpPost("{id:guid}/instructors/{instructorId:guid}")]
        [ProducesResponseType(typeof(CourseInstructorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignInstructor(
            Guid id,
            Guid instructorId,
            CancellationToken cancellationToken)
        {
            return await SendCourseRequest(
                new AssignInstructorToCourseCommand(id, instructorId),
                cancellationToken);
        }

        /// <summary>
        /// Remove an instructor from a course. Admin only.
        /// </summary>
        [HttpDelete("{id:guid}/instructors/{instructorId:guid}")]
        [ProducesResponseType(typeof(CourseMutationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveInstructor(
            Guid id,
            Guid instructorId,
            CancellationToken cancellationToken)
        {
            return await SendCourseRequest(
                new RemoveInstructorFromCourseCommand(id, instructorId),
                cancellationToken);
        }

        /// <summary>
        /// List courses with optional filters. Admin only.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<CourseListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status,
            [FromQuery] string? category,
            [FromQuery] string? search,
            [FromQuery] int page = PaginationDefaults.Page,
            [FromQuery] int pageSize = PaginationDefaults.PageSize,
            CancellationToken cancellationToken = default)
        {
            return await SendCourseRequest(new GetCoursesQuery(status, category, search, page, pageSize), cancellationToken);
        }

        /// <summary>
        /// Get course details. Admin only.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            return await SendCourseRequest(new GetCourseByIdQuery(id), cancellationToken);
        }

        /// <summary>
        /// Get instructors assigned to a course. Admin only.
        /// </summary>
        [HttpGet("{id:guid}/instructors")]
        [ProducesResponseType(typeof(IReadOnlyList<CourseInstructorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInstructors(Guid id, CancellationToken cancellationToken)
        {
            return await SendCourseRequest(new GetCourseInstructorsQuery(id), cancellationToken);
        }

        private async Task<IActionResult> SendCourseRequest<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken)
        {
            // Exceptions are translated centrally by ExceptionHandlingMiddleware.
            var response = await _sender.Send(request, cancellationToken);
            return Ok(response);
        }

        public record CreateCourseRequest(
            string Title,
            string Slug,
            string? Description,
            string? ThumbnailUrl,
            string? Category,
            decimal Price,
            string Currency);

        public record UpdateCourseRequest(
            string Title,
            string Slug,
            string? Description,
            string? ThumbnailUrl,
            string? Category,
            decimal Price,
            string Currency,
            decimal? CompletionAttendanceThreshold);
    }
}
