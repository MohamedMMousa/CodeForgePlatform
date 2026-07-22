using CodeForge.Application.Analytics.GetAdminAcademicDashboard;
using CodeForge.Application.Analytics.GetAdminBusinessDashboard;
using CodeForge.Application.Analytics.GetInstructorDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("analytics")]
    [Produces("application/json")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ISender _sender;

        public AnalyticsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Admin business dashboard: enrollments, revenue, leads, cohorts.</summary>
        [HttpGet("admin/business")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> AdminBusiness(CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetAdminBusinessDashboardQuery(), cancellationToken);
            return Ok(response);
        }

        /// <summary>Admin academic dashboard: assessments, pass rates, certificates per course.</summary>
        [HttpGet("admin/academic")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> AdminAcademic(CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetAdminAcademicDashboardQuery(), cancellationToken);
            return Ok(response);
        }

        /// <summary>The current instructor's analytics across their assigned courses.</summary>
        [HttpGet("instructor")]
        [Authorize(Policy = "InstructorOnly")]
        public async Task<IActionResult> Instructor(CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetInstructorDashboardQuery(), cancellationToken);
            return Ok(response);
        }
    }
}
