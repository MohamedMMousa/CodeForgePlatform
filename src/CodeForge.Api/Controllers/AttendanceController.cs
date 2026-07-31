using CodeForge.Application.Attendance.Common;
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
        [ProducesResponseType(typeof(AttendanceResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Mark(Guid sessionId, MarkAttendanceRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new MarkAttendanceCommand(sessionId, request.Entries), cancellationToken);
            return Ok(response);
        }

        [HttpGet("sessions/{sessionId:guid}/attendance")]
        [ProducesResponseType(typeof(SessionRosterDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoster(Guid sessionId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetSessionRosterQuery(sessionId), cancellationToken);
            return Ok(response);
        }

        [HttpGet("courses/{courseId:guid}/attendance-report")]
        [ProducesResponseType(typeof(CourseAttendanceReportDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCourseReport(Guid courseId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetCourseAttendanceReportQuery(courseId), cancellationToken);
            return Ok(response);
        }

        [HttpGet("my-courses/{courseId:guid}/attendance")]
        [ProducesResponseType(typeof(MyAttendanceDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyAttendance(Guid courseId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetMyAttendanceQuery(courseId), cancellationToken);
            return Ok(response);
        }

        public record MarkAttendanceRequest(List<AttendanceEntryDto> Entries);
    }
}
