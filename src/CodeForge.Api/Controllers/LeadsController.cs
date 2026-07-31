using CodeForge.Application.Leads.Common;
using CodeForge.Application.Leads.GetLeads;
using CodeForge.Application.Leads.MarkLeadContacted;
using CodeForge.Application.Leads.SubmitLead;
using CodeForge.Api.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Route("leads")]
    [Produces("application/json")]
    public class LeadsController : ControllerBase
    {
        private readonly ISender _sender;

        public LeadsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Public contact form / "notify me about the next batch" submission.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.PublicSubmit)]
        [HttpPost]
        [ProducesResponseType(typeof(LeadDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Submit(SubmitLeadRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new SubmitLeadCommand(request.Name, request.Email, request.Phone, request.Message, request.CourseId),
                cancellationToken);
            return Ok(response);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<LeadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool? isContacted, [FromQuery] Guid? courseId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetLeadsQuery(isContacted, courseId), cancellationToken);
            return Ok(response);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{id:guid}/mark-contacted")]
        [ProducesResponseType(typeof(LeadDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkContacted(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new MarkLeadContactedCommand(id), cancellationToken);
            return Ok(response);
        }

        public record SubmitLeadRequest(string Name, string Email, string? Phone, string? Message, Guid? CourseId);
    }
}
