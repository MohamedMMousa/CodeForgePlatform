using CodeForge.Application.Sessions.Common;
using CodeForge.Application.Sessions.CreateSession;
using CodeForge.Application.Sessions.DeleteSession;
using CodeForge.Application.Sessions.GetModuleSessions;
using CodeForge.Application.Sessions.GetSessionById;
using CodeForge.Application.Sessions.ReorderSessions;
using CodeForge.Application.Sessions.UpdateSession;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class SessionsController : ControllerBase
    {
        private readonly ISender _sender;

        public SessionsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("modules/{moduleId:guid}/sessions")]
        [ProducesResponseType(typeof(SessionResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create(Guid moduleId, CreateSessionRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new CreateSessionCommand(
                    moduleId, request.Type, request.Title, request.Description, request.ScheduledAt,
                    request.DurationMinutes, request.JoinLink, request.Location, request.VideoUrl,
                    request.InstructorId),
                cancellationToken);
            return Ok(response);
        }

        [HttpPut("sessions/{id:guid}")]
        [ProducesResponseType(typeof(SessionResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, UpdateSessionRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new UpdateSessionCommand(
                    id, request.Type, request.Title, request.Description, request.ScheduledAt,
                    request.DurationMinutes, request.JoinLink, request.Location, request.VideoUrl,
                    request.InstructorId),
                cancellationToken);
            return Ok(response);
        }

        [HttpDelete("sessions/{id:guid}")]
        [ProducesResponseType(typeof(SessionResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new DeleteSessionCommand(id), cancellationToken);
            return Ok(response);
        }

        [HttpPut("modules/{moduleId:guid}/sessions/reorder")]
        public async Task<IActionResult> Reorder(Guid moduleId, ReorderSessionsRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new ReorderSessionsCommand(moduleId, request.SessionOrders), cancellationToken);
            return NoContent();
        }

        [HttpGet("modules/{moduleId:guid}/sessions")]
        [ProducesResponseType(typeof(IReadOnlyList<SessionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetForModule(Guid moduleId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetModuleSessionsQuery(moduleId), cancellationToken);
            return Ok(response);
        }

        [HttpGet("sessions/{id:guid}")]
        [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetSessionByIdQuery(id), cancellationToken);
            return Ok(response);
        }

        public record CreateSessionRequest(
            string Type, string Title, string? Description, DateTime? ScheduledAt, int? DurationMinutes,
            string? JoinLink, string? Location, string? VideoUrl, Guid? InstructorId);

        public record UpdateSessionRequest(
            string Type, string Title, string? Description, DateTime? ScheduledAt, int? DurationMinutes,
            string? JoinLink, string? Location, string? VideoUrl, Guid? InstructorId);

        public record ReorderSessionsRequest(List<SessionOrderDto> SessionOrders);
    }
}
