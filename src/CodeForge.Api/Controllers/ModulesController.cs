using CodeForge.Application.Modules.Common;
using CodeForge.Application.Modules.CreateModule;
using CodeForge.Application.Modules.DeleteModule;
using CodeForge.Application.Modules.GetCourseModules;
using CodeForge.Application.Modules.GetModuleById;
using CodeForge.Application.Modules.ReorderModules;
using CodeForge.Application.Modules.UpdateModule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class ModulesController : ControllerBase
    {
        private readonly ISender _sender;

        public ModulesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("courses/{courseId:guid}/modules")]
        [ProducesResponseType(typeof(ModuleResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create(Guid courseId, CreateModuleRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new CreateModuleCommand(courseId, request.Title, request.Description), cancellationToken);
            return Ok(response);
        }

        [HttpPut("modules/{id:guid}")]
        [ProducesResponseType(typeof(ModuleResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, UpdateModuleRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new UpdateModuleCommand(id, request.Title, request.Description), cancellationToken);
            return Ok(response);
        }

        [HttpDelete("modules/{id:guid}")]
        [ProducesResponseType(typeof(ModuleResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new DeleteModuleCommand(id), cancellationToken);
            return Ok(response);
        }

        [HttpPut("courses/{courseId:guid}/modules/reorder")]
        public async Task<IActionResult> Reorder(Guid courseId, ReorderModulesRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new ReorderModulesCommand(courseId, request.ModuleOrders), cancellationToken);
            return NoContent();
        }

        [HttpGet("courses/{courseId:guid}/modules")]
        [ProducesResponseType(typeof(IReadOnlyList<ModuleListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetForCourse(Guid courseId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetCourseModulesQuery(courseId), cancellationToken);
            return Ok(response);
        }

        [HttpGet("modules/{id:guid}")]
        [ProducesResponseType(typeof(ModuleListDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetModuleByIdQuery(id), cancellationToken);
            return Ok(response);
        }

        public record CreateModuleRequest(string Title, string? Description);
        public record UpdateModuleRequest(string Title, string? Description);
        public record ReorderModulesRequest(List<ModuleOrderDto> ModuleOrders);
    }
}
