using CodeForge.Application.Materials.CreateMaterial;
using CodeForge.Application.Materials.DeleteMaterial;
using CodeForge.Application.Materials.GetMaterialFile;
using CodeForge.Application.Materials.GetModuleMaterials;
using CodeForge.Application.Materials.GetSessionMaterials;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class MaterialsController : ControllerBase
    {
        private readonly ISender _sender;

        public MaterialsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("modules/{moduleId:guid}/materials")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> CreateForModule(
            Guid moduleId, [FromForm] CreateMaterialForm form, CancellationToken cancellationToken)
        {
            return await Create(moduleId, null, form, cancellationToken);
        }

        [HttpPost("sessions/{sessionId:guid}/materials")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> CreateForSession(
            Guid sessionId, [FromForm] CreateMaterialForm form, CancellationToken cancellationToken)
        {
            return await Create(null, sessionId, form, cancellationToken);
        }

        private async Task<IActionResult> Create(
            Guid? moduleId, Guid? sessionId, CreateMaterialForm form, CancellationToken cancellationToken)
        {
            Stream? fileStream = null;
            try
            {
                if (form.File is not null)
                {
                    fileStream = form.File.OpenReadStream();
                }

                var response = await _sender.Send(
                    new CreateMaterialCommand(
                        moduleId, sessionId, form.Type, form.Title, form.Body, form.LinkUrl,
                        form.FileType, fileStream, form.File?.FileName, form.File?.ContentType),
                    cancellationToken);

                return Ok(response);
            }
            finally
            {
                if (fileStream is not null)
                {
                    await fileStream.DisposeAsync();
                }
            }
        }

        [HttpDelete("materials/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new DeleteMaterialCommand(id), cancellationToken);
            return Ok(response);
        }

        /// <summary>
        /// Download a material's file. Enrollment-gated — same rule as viewing the
        /// material's metadata (admin, assigned instructor, or actively enrolled student).
        /// </summary>
        [HttpGet("materials/{id:guid}/file")]
        public async Task<IActionResult> GetFile(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetMaterialFileQuery(id), cancellationToken);
            return File(result.Stream, result.ContentType, result.FileName);
        }

        [HttpGet("modules/{moduleId:guid}/materials")]
        public async Task<IActionResult> GetForModule(Guid moduleId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetModuleMaterialsQuery(moduleId), cancellationToken);
            return Ok(response);
        }

        [HttpGet("sessions/{sessionId:guid}/materials")]
        public async Task<IActionResult> GetForSession(Guid sessionId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetSessionMaterialsQuery(sessionId), cancellationToken);
            return Ok(response);
        }

        public class CreateMaterialForm
        {
            public string Type { get; set; } = null!;
            public string Title { get; set; } = null!;
            public string? Body { get; set; }
            public string? LinkUrl { get; set; }
            public string? FileType { get; set; }
            public IFormFile? File { get; set; }
        }
    }
}
