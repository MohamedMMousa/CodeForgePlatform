using CodeForge.Application.Announcements.CreateAnnouncement;
using CodeForge.Application.Announcements.DeleteAnnouncement;
using CodeForge.Application.Announcements.GetAnnouncements;
using CodeForge.Application.Announcements.UpdateAnnouncement;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Route("announcements")]
    [Authorize]
    [Produces("application/json")]
    public class AnnouncementsController : ControllerBase
    {
        private readonly ISender _sender;

        public AnnouncementsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateAnnouncementRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new CreateAnnouncementCommand(request.CourseId, request.Title, request.Body), cancellationToken);
            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateAnnouncementRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new UpdateAnnouncementCommand(id, request.Title, request.Body), cancellationToken);
            return Ok(response);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new DeleteAnnouncementCommand(id), cancellationToken);
            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? courseId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetAnnouncementsQuery(courseId), cancellationToken);
            return Ok(response);
        }

        public record CreateAnnouncementRequest(Guid? CourseId, string Title, string Body);
        public record UpdateAnnouncementRequest(string Title, string Body);
    }
}
