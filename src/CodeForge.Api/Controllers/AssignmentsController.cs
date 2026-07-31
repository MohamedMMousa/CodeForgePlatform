using CodeForge.Application.Assignments.AddTestCase;
using CodeForge.Application.Assignments.Common;
using CodeForge.Application.Assignments.CreateAssignment;
using CodeForge.Application.Assignments.DeleteAssignment;
using CodeForge.Application.Assignments.DeleteTestCase;
using CodeForge.Application.Assignments.GetAssignmentById;
using CodeForge.Application.Assignments.GetAssignmentForSubmission;
using CodeForge.Application.Assignments.GetModuleAssignments;
using CodeForge.Application.Assignments.GetMySubmissions;
using CodeForge.Application.Assignments.GetSubmissionResult;
using CodeForge.Application.Assignments.GetSubmissionsForGrading;
using CodeForge.Application.Assignments.GradeSubmission;
using CodeForge.Application.Assignments.ReorderAssignments;
using CodeForge.Application.Assignments.ReorderTestCases;
using CodeForge.Application.Assignments.SubmitAssignment;
using CodeForge.Application.Assignments.UpdateAssignment;
using CodeForge.Application.Assignments.UpdateTestCase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class AssignmentsController : ControllerBase
    {
        private readonly ISender _sender;

        public AssignmentsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("modules/{moduleId:guid}/assignments")]
        [ProducesResponseType(typeof(AssignmentResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create(Guid moduleId, CreateAssignmentRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new CreateAssignmentCommand(
                    moduleId, request.Title, request.Description, request.IsPractice,
                    request.MaxAttempts, request.DueAt, request.PassScore),
                cancellationToken);
            return Ok(response);
        }

        [HttpPut("assignments/{id:guid}")]
        [ProducesResponseType(typeof(AssignmentResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, UpdateAssignmentRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new UpdateAssignmentCommand(
                    id, request.Title, request.Description, request.IsPractice,
                    request.MaxAttempts, request.DueAt, request.PassScore),
                cancellationToken);
            return Ok(response);
        }

        [HttpDelete("assignments/{id:guid}")]
        [ProducesResponseType(typeof(AssignmentResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new DeleteAssignmentCommand(id), cancellationToken);
            return Ok(response);
        }

        [HttpPut("modules/{moduleId:guid}/assignments/reorder")]
        public async Task<IActionResult> Reorder(Guid moduleId, ReorderAssignmentsRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new ReorderAssignmentsCommand(moduleId, request.AssignmentOrders), cancellationToken);
            return NoContent();
        }

        [HttpGet("modules/{moduleId:guid}/assignments")]
        [ProducesResponseType(typeof(IReadOnlyList<AssignmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetForModule(Guid moduleId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetModuleAssignmentsQuery(moduleId), cancellationToken);
            return Ok(response);
        }

        [HttpGet("assignments/{id:guid}")]
        [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetAssignmentByIdQuery(id), cancellationToken);
            return Ok(response);
        }

        [HttpPost("assignments/{assignmentId:guid}/test-cases")]
        [ProducesResponseType(typeof(TestCaseResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddTestCase(Guid assignmentId, AddTestCaseRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new AddTestCaseCommand(assignmentId, request.Input, request.ExpectedOutput, request.IsHidden, request.Points),
                cancellationToken);
            return Ok(response);
        }

        [HttpPut("test-cases/{id:guid}")]
        [ProducesResponseType(typeof(TestCaseResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateTestCase(Guid id, UpdateTestCaseRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new UpdateTestCaseCommand(id, request.Input, request.ExpectedOutput, request.IsHidden, request.Points),
                cancellationToken);
            return Ok(response);
        }

        [HttpDelete("test-cases/{id:guid}")]
        [ProducesResponseType(typeof(TestCaseResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteTestCase(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new DeleteTestCaseCommand(id), cancellationToken);
            return Ok(response);
        }

        [HttpPut("assignments/{assignmentId:guid}/test-cases/reorder")]
        public async Task<IActionResult> ReorderTestCases(Guid assignmentId, ReorderTestCasesRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new ReorderTestCasesCommand(assignmentId, request.TestCaseOrders), cancellationToken);
            return NoContent();
        }

        [HttpGet("assignments/{id:guid}/submission")]
        [ProducesResponseType(typeof(AssignmentForSubmissionDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetForSubmission(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetAssignmentForSubmissionQuery(id), cancellationToken);
            return Ok(response);
        }

        [HttpPost("assignments/{id:guid}/submissions")]
        [ProducesResponseType(typeof(SubmissionResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Submit(Guid id, SubmitAssignmentRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new SubmitAssignmentCommand(id, request.Code), cancellationToken);
            return Ok(response);
        }

        [HttpPut("submissions/{submissionId:guid}/grade")]
        [ProducesResponseType(typeof(SubmissionResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Grade(Guid submissionId, GradeSubmissionRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new GradeSubmissionCommand(submissionId, request.ManualScore, request.ManualFeedback), cancellationToken);
            return Ok(response);
        }

        [HttpGet("assignments/{id:guid}/my-submissions")]
        [ProducesResponseType(typeof(IReadOnlyList<SubmissionSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMySubmissions(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetMySubmissionsQuery(id), cancellationToken);
            return Ok(response);
        }

        [HttpGet("submissions/{submissionId:guid}")]
        [ProducesResponseType(typeof(SubmissionResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSubmissionResult(Guid submissionId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetSubmissionResultQuery(submissionId), cancellationToken);
            return Ok(response);
        }

        [HttpGet("assignments/{id:guid}/submissions")]
        [ProducesResponseType(typeof(AssignmentSubmissionsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSubmissionsForGrading(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetSubmissionsForGradingQuery(id), cancellationToken);
            return Ok(response);
        }

        public record CreateAssignmentRequest(
            string Title, string Description, bool IsPractice, int? MaxAttempts, DateTime? DueAt, int? PassScore);

        public record UpdateAssignmentRequest(
            string Title, string Description, bool IsPractice, int? MaxAttempts, DateTime? DueAt, int? PassScore);

        public record ReorderAssignmentsRequest(List<AssignmentOrderDto> AssignmentOrders);

        public record AddTestCaseRequest(string Input, string ExpectedOutput, bool IsHidden, int Points);

        public record UpdateTestCaseRequest(string Input, string ExpectedOutput, bool IsHidden, int Points);

        public record ReorderTestCasesRequest(List<TestCaseOrderDto> TestCaseOrders);

        public record SubmitAssignmentRequest(string Code);

        public record GradeSubmissionRequest(int ManualScore, string? ManualFeedback);
    }
}
