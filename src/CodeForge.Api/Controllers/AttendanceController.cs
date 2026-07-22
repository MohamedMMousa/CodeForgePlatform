using CodeForge.Application.Attendance.GetCourseAttendanceReport;
using CodeForge.Application.Attendance.GetMyAttendance;
using CodeForge.Application.Attendance.GetSessionRoster;
using CodeForge.Application.Attendance.MarkAttendance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class AttendanceController : ControllerBase
    {
        private readonly ISender _sender;

        public AttendanceController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPut("sessions/{sessionId:guid}/attendance")]
        public async Task<IActionResult> Mark(Guid sessionId, MarkAttendanceRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new MarkAttendanceCommand(sessionId, request.Entries), cancellationToken);
            return Ok(response);
        }

        [HttpGet("sessions/{sessionId:guid}/attendance")]
        public async Task<IActionResult> GetRoster(Guid sessionId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetSessionRosterQuery(sessionId), cancellationToken);
            return Ok(response);
        }

        [HttpGet("courses/{courseId:guid}/attendance-report")]
        public async Task<IActionResult> GetCourseReport(Guid courseId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetCourseAttendanceReportQuery(courseId), cancellationToken);
            return Ok(response);
        }

        [HttpGet("my-courses/{courseId:guid}/attendance")]
        public async Task<IActionResult> GetMyAttendance(Guid courseId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetMyAttendanceQuery(courseId), cancellationToken);
            return Ok(response);
        }

        public record MarkAttendanceRequest(List<AttendanceEntryDto> Entries);
    }
}
