using CodeForge.Application.Cohorts.CancelCohort;
using CodeForge.Application.Cohorts.Common;
using CodeForge.Application.Cohorts.CompleteCohort;
using CodeForge.Application.Cohorts.CreateCohort;
using CodeForge.Application.Cohorts.GetCohortById;
using CodeForge.Application.Cohorts.GetCourseCohorts;
using CodeForge.Application.Cohorts.OpenCohort;
using CodeForge.Application.Cohorts.UpdateCohort;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    [Produces("application/json")]
    public class CohortsController : ControllerBase
    {
        private readonly ISender _sender;

        public CohortsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("courses/{courseId:guid}/cohorts")]
        [ProducesResponseType(typeof(CohortListDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create(Guid courseId, CreateCohortRequest request, CancellationToken cancellationToken)
        {
            return await SendCohortRequest(
                new CreateCohortCommand(
                    courseId, request.Name, request.StartDate, request.EndDate,
                    request.EnrollmentCutoffDate, request.Capacity, request.GracePeriodDays),
                cancellationToken);
        }

        [HttpPut("cohorts/{id:guid}")]
        [ProducesResponseType(typeof(CohortListDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, UpdateCohortRequest request, CancellationToken cancellationToken)
        {
            return await SendCohortRequest(
                new UpdateCohortCommand(
                    id, request.Name, request.StartDate, request.EndDate,
                    request.EnrollmentCutoffDate, request.Capacity, request.GracePeriodDays),
                cancellationToken);
        }

        [HttpPut("cohorts/{id:guid}/open")]
        [ProducesResponseType(typeof(CohortMutationResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Open(Guid id, CancellationToken cancellationToken)
        {
            return await SendCohortRequest(new OpenCohortCommand(id), cancellationToken);
        }

        [HttpPut("cohorts/{id:guid}/cancel")]
        [ProducesResponseType(typeof(CohortMutationResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        {
            return await SendCohortRequest(new CancelCohortCommand(id), cancellationToken);
        }

        [HttpPut("cohorts/{id:guid}/complete")]
        [ProducesResponseType(typeof(CohortMutationResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
        {
            return await SendCohortRequest(new CompleteCohortCommand(id), cancellationToken);
        }

        [HttpGet("courses/{courseId:guid}/cohorts")]
        [ProducesResponseType(typeof(IReadOnlyList<CohortListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetForCourse(Guid courseId, CancellationToken cancellationToken)
        {
            return await SendCohortRequest(new GetCourseCohortsQuery(courseId), cancellationToken);
        }

        [HttpGet("cohorts/{id:guid}")]
        [ProducesResponseType(typeof(CohortListDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            return await SendCohortRequest(new GetCohortByIdQuery(id), cancellationToken);
        }

        private async Task<IActionResult> SendCohortRequest<TResponse>(
            IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(request, cancellationToken);
            return Ok(response);
        }

        public record CreateCohortRequest(
            string Name,
            DateTime StartDate,
            DateTime EndDate,
            DateTime EnrollmentCutoffDate,
            int Capacity,
            int GracePeriodDays);

        public record UpdateCohortRequest(
            string Name,
            DateTime StartDate,
            DateTime EndDate,
            DateTime EnrollmentCutoffDate,
            int Capacity,
            int GracePeriodDays);
    }
}
