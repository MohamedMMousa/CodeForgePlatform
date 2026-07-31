using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using CodeForge.Application.EnrollmentRequests.ApproveEnrollmentRequest;
using CodeForge.Application.EnrollmentRequests.Common;
using CodeForge.Application.EnrollmentRequests.GetEnrollmentRequestById;
using CodeForge.Application.EnrollmentRequests.GetEnrollmentRequests;
using CodeForge.Application.EnrollmentRequests.GetPaymentProofFile;
using CodeForge.Application.EnrollmentRequests.RejectEnrollmentRequest;
using CodeForge.Application.EnrollmentRequests.SubmitEnrollmentRequest;
using CodeForge.Api.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Route("enrollment-requests")]
    [Produces("application/json")]
    public class EnrollmentRequestsController : ControllerBase
    {
        private readonly ISender _sender;

        public EnrollmentRequestsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Submit a public enrollment request with payment proof upload.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.PublicSubmit)]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(EnrollmentRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Submit(
            [FromForm] SubmitEnrollmentRequestForm request,
            CancellationToken cancellationToken)
        {
            if (request.PaymentProof is null)
            {
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["PaymentProof"] = new[] { "Payment proof file is required." }
                }));
            }

            await using var paymentProofStream = request.PaymentProof.OpenReadStream();

            return await SendEnrollmentRequest(
                new SubmitEnrollmentRequestCommand(
                    request.FullName,
                    request.Email,
                    request.PhoneNumber,
                    request.CourseId,
                    request.TrackId,
                    request.PaymentMethod,
                    request.CouponCode,
                    paymentProofStream,
                    request.PaymentProof.FileName,
                    request.PaymentProof.ContentType),
                cancellationToken);
        }

        /// <summary>
        /// List enrollment requests. Admin only.
        /// </summary>
        [Authorize(Policy = "AdminOnly")]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<EnrollmentRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status,
            [FromQuery] Guid? courseId,
            [FromQuery] Guid? trackId,
            [FromQuery] int page = PaginationDefaults.Page,
            [FromQuery] int pageSize = PaginationDefaults.PageSize,
            CancellationToken cancellationToken = default)
        {
            return await SendEnrollmentRequest(
                new GetEnrollmentRequestsQuery(status, courseId, trackId, page, pageSize),
                cancellationToken);
        }

        /// <summary>
        /// Get enrollment request details, including payment proof URL. Admin only.
        /// </summary>
        [Authorize(Policy = "AdminOnly")]
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(EnrollmentRequestDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            return await SendEnrollmentRequest(
                new GetEnrollmentRequestByIdQuery(id),
                cancellationToken);
        }

        /// <summary>
        /// Download the payment proof file for an enrollment request. Admin only.
        /// </summary>
        [Authorize(Policy = "AdminOnly")]
        [HttpGet("{id:guid}/payment-proof")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPaymentProof(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetPaymentProofFileQuery(id), cancellationToken);
            return File(result.Stream, result.ContentType, result.FileName);
        }

        /// <summary>
        /// Approve an enrollment request. Creates a student account if needed and creates an enrollment. Admin only.
        /// </summary>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{id:guid}/approve")]
        [ProducesResponseType(typeof(EnrollmentApprovalResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
        {
            return await SendEnrollmentRequest(
                new ApproveEnrollmentRequestCommand(id),
                cancellationToken);
        }

        /// <summary>
        /// Reject an enrollment request with a reason. Admin only.
        /// </summary>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{id:guid}/reject")]
        [ProducesResponseType(typeof(EnrollmentRequestMessageDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reject(
            Guid id,
            RejectEnrollmentRequestBody request,
            CancellationToken cancellationToken)
        {
            return await SendEnrollmentRequest(
                new RejectEnrollmentRequestCommand(id, request.RejectionReason),
                cancellationToken);
        }

        private async Task<IActionResult> SendEnrollmentRequest<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken)
        {
            // Exceptions are translated centrally by ExceptionHandlingMiddleware.
            var response = await _sender.Send(request, cancellationToken);
            return Ok(response);
        }

        public class SubmitEnrollmentRequestForm
        {
            public string FullName { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string? PhoneNumber { get; set; }
            public Guid? CourseId { get; set; }
            public Guid? TrackId { get; set; }
            public string PaymentMethod { get; set; } = null!;
            public string? CouponCode { get; set; }
            public IFormFile? PaymentProof { get; set; }
        }

        public record RejectEnrollmentRequestBody(string RejectionReason);
    }
}
