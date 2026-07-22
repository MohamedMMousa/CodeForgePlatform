using CodeForge.Application.Assessments.Common;
using CodeForge.Application.Assessments.CreateAssessment;
using CodeForge.Application.Assessments.CreateQuestion;
using CodeForge.Application.Assessments.DeleteAssessment;
using CodeForge.Application.Assessments.DeleteQuestion;
using CodeForge.Application.Assessments.GetAssessmentById;
using CodeForge.Application.Assessments.GetAssessmentForAttempt;
using CodeForge.Application.Assessments.GetAssessmentResults;
using CodeForge.Application.Assessments.GetAttemptResult;
using CodeForge.Application.Assessments.GetModuleAssessments;
using CodeForge.Application.Assessments.GetMyAttempts;
using CodeForge.Application.Assessments.ReorderAssessments;
using CodeForge.Application.Assessments.ReorderQuestions;
using CodeForge.Application.Assessments.StartAttempt;
using CodeForge.Application.Assessments.SubmitAttempt;
using CodeForge.Application.Assessments.UpdateAssessment;
using CodeForge.Application.Assessments.UpdateQuestion;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class AssessmentsController : ControllerBase
    {
        private readonly ISender _sender;

        public AssessmentsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("modules/{moduleId:guid}/assessments")]
        public async Task<IActionResult> Create(Guid moduleId, CreateAssessmentRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new CreateAssessmentCommand(
                    moduleId, request.Type, request.Title, request.TimeLimitMinutes, request.PassScore,
                    request.IsPractice, request.MaxAttempts, request.RandomizeQuestions, request.DisableCopyPaste),
                cancellationToken);
            return Ok(response);
        }

        [HttpPut("assessments/{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateAssessmentRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new UpdateAssessmentCommand(
                    id, request.Type, request.Title, request.TimeLimitMinutes, request.PassScore,
                    request.IsPractice, request.MaxAttempts, request.RandomizeQuestions, request.DisableCopyPaste),
                cancellationToken);
            return Ok(response);
        }

        [HttpDelete("assessments/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new DeleteAssessmentCommand(id), cancellationToken);
            return Ok(response);
        }

        [HttpPut("modules/{moduleId:guid}/assessments/reorder")]
        public async Task<IActionResult> Reorder(Guid moduleId, ReorderAssessmentsRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new ReorderAssessmentsCommand(moduleId, request.AssessmentOrders), cancellationToken);
            return NoContent();
        }

        [HttpGet("modules/{moduleId:guid}/assessments")]
        public async Task<IActionResult> GetForModule(Guid moduleId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetModuleAssessmentsQuery(moduleId), cancellationToken);
            return Ok(response);
        }

        [HttpGet("assessments/{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetAssessmentByIdQuery(id), cancellationToken);
            return Ok(response);
        }

        [HttpPost("assessments/{assessmentId:guid}/questions")]
        public async Task<IActionResult> CreateQuestion(Guid assessmentId, CreateQuestionRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new CreateQuestionCommand(assessmentId, request.QuestionText, request.Options), cancellationToken);
            return Ok(response);
        }

        [HttpPut("questions/{id:guid}")]
        public async Task<IActionResult> UpdateQuestion(Guid id, UpdateQuestionRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new UpdateQuestionCommand(id, request.QuestionText, request.Options), cancellationToken);
            return Ok(response);
        }

        [HttpDelete("questions/{id:guid}")]
        public async Task<IActionResult> DeleteQuestion(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new DeleteQuestionCommand(id), cancellationToken);
            return Ok(response);
        }

        [HttpPut("assessments/{assessmentId:guid}/questions/reorder")]
        public async Task<IActionResult> ReorderQuestions(Guid assessmentId, ReorderQuestionsRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new ReorderQuestionsCommand(assessmentId, request.QuestionOrders), cancellationToken);
            return NoContent();
        }

        [HttpGet("assessments/{id:guid}/attempt")]
        public async Task<IActionResult> GetForAttempt(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetAssessmentForAttemptQuery(id), cancellationToken);
            return Ok(response);
        }

        [HttpPost("assessments/{id:guid}/attempts")]
        public async Task<IActionResult> StartAttempt(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new StartAttemptCommand(id), cancellationToken);
            return Ok(response);
        }

        [HttpPut("attempts/{attemptId:guid}/submit")]
        public async Task<IActionResult> SubmitAttempt(Guid attemptId, SubmitAttemptRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new SubmitAttemptCommand(attemptId, request.Answers), cancellationToken);
            return Ok(response);
        }

        [HttpGet("assessments/{id:guid}/my-attempts")]
        public async Task<IActionResult> GetMyAttempts(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetMyAttemptsQuery(id), cancellationToken);
            return Ok(response);
        }

        [HttpGet("attempts/{attemptId:guid}")]
        public async Task<IActionResult> GetAttemptResult(Guid attemptId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetAttemptResultQuery(attemptId), cancellationToken);
            return Ok(response);
        }

        [HttpGet("assessments/{id:guid}/results")]
        public async Task<IActionResult> GetResults(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetAssessmentResultsQuery(id), cancellationToken);
            return Ok(response);
        }

        public record CreateAssessmentRequest(
            string Type, string Title, int? TimeLimitMinutes, int? PassScore, bool IsPractice,
            int? MaxAttempts, bool RandomizeQuestions, bool DisableCopyPaste);

        public record UpdateAssessmentRequest(
            string Type, string Title, int? TimeLimitMinutes, int? PassScore, bool IsPractice,
            int? MaxAttempts, bool RandomizeQuestions, bool DisableCopyPaste);

        public record ReorderAssessmentsRequest(List<AssessmentOrderDto> AssessmentOrders);

        public record CreateQuestionRequest(string QuestionText, List<OptionInputDto> Options);

        public record UpdateQuestionRequest(string QuestionText, List<OptionInputDto> Options);

        public record ReorderQuestionsRequest(List<QuestionOrderDto> QuestionOrders);

        public record SubmitAttemptRequest(List<AnswerInputDto> Answers);
    }
}
