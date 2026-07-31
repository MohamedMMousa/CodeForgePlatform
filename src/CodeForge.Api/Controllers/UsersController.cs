using CodeForge.Application.Users.Common;
using CodeForge.Application.Users.CreateInstructor;
using CodeForge.Application.Users.DeactivateUser;
using CodeForge.Application.Users.GetUsers;
using CodeForge.Application.Users.ReactivateUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Route("users")]
    [Authorize(Policy = "AdminOnly")]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly ISender _sender;

        public UsersController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? role,
            [FromQuery] bool? isActive,
            [FromQuery] string? search,
            CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetUsersQuery(role, isActive, search), cancellationToken);
            return Ok(response);
        }

        [HttpPost("instructors")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateInstructor(
            CreateInstructorCommand command, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(command, cancellationToken);
            return Ok(response);
        }

        [HttpPut("{id:guid}/deactivate")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new DeactivateUserCommand(id), cancellationToken);
            return Ok(response);
        }

        [HttpPut("{id:guid}/reactivate")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new ReactivateUserCommand(id), cancellationToken);
            return Ok(response);
        }
    }
}
