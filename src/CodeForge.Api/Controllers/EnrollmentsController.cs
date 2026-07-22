using CodeForge.Application.Enrollments.CancelEnrollment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Route("enrollments")]
    [Authorize(Policy = "AdminOnly")]
    [Produces("application/json")]
    public class EnrollmentsController : ControllerBase
    {
        private readonly ISender _sender;

        public EnrollmentsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Cancel an active enrollment and revoke access. Admin only.
        /// </summary>
        [HttpPut("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancelEnrollmentRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new CancelEnrollmentCommand(id, request.Reason, request.MarkAsRefunded),
                cancellationToken);
            return Ok(response);
        }

        public record CancelEnrollmentRequest(string Reason, bool MarkAsRefunded);
    }
}
