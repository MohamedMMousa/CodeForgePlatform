using CodeForge.Application.Assignments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assignments.SubmitAssignment
{
    public class SubmitAssignmentCommandHandler : IRequestHandler<SubmitAssignmentCommand, SubmissionResultDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICodeExecutionService _codeExecutionService;

        public SubmitAssignmentCommandHandler(
            ICodeForgeDbContext context,
            ICurrentUserService currentUserService,
            ICodeExecutionService codeExecutionService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _codeExecutionService = codeExecutionService;
        }

        public async Task<SubmissionResultDto> Handle(SubmitAssignmentCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var assignment = await _context.Assignments
                .Include(a => a.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(a => a.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Enrollments)
                .Include(a => a.TestCases)
                .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);

            if (assignment is null)
            {
                throw new KeyNotFoundException("Assignment was not found.");
            }

            CourseContentAuthorization.EnsureCanView(_currentUserService, assignment.Module.Course, currentUserId);

            if (_currentUserService.Role != Roles.Student)
            {
                throw new InvalidOperationException("Only students can submit an assignment.");
            }

            var attemptsUsed = await _context.AssignmentSubmissions
                .CountAsync(s => s.AssignmentId == assignment.Id && s.StudentId == currentUserId, cancellationToken);

            if (assignment.MaxAttempts.HasValue && attemptsUsed >= assignment.MaxAttempts.Value)
            {
                throw new InvalidOperationException("Maximum attempts reached for this assignment.");
            }

            var isLate = assignment.DueAt.HasValue && DateTime.UtcNow > assignment.DueAt.Value;

            var submission = new AssignmentSubmission
            {
                AssignmentId = assignment.Id,
                StudentId = currentUserId,
                Code = request.Code,
                AttemptNumber = attemptsUsed + 1,
                IsLate = isLate,
                AutoGradingStatus = AssignmentGradingStatuses.Pending,
            };

            if (assignment.TestCases.Count > 0)
            {
                try
                {
                    var testCaseInputs = assignment.TestCases
                        .Select(tc => new TestCaseExecutionInput(tc.Id, tc.Input, tc.ExpectedOutput))
                        .ToList();

                    var executionResults = await _codeExecutionService.RunAsync(
                        request.Code, "python", testCaseInputs, cancellationToken);

                    var outcomes = new List<AssignmentGradingCalculator.TestCaseOutcome>();
                    var testResults = new List<AssignmentTestResult>();
                    foreach (var testCase in assignment.TestCases)
                    {
                        var result = executionResults.FirstOrDefault(r => r.TestCaseId == testCase.Id);
                        var passed = result?.Passed ?? false;
                        outcomes.Add(new AssignmentGradingCalculator.TestCaseOutcome(testCase.Points, passed));

                        testResults.Add(new AssignmentTestResult
                        {
                            TestCaseId = testCase.Id,
                            TestCase = testCase,
                            Passed = passed,
                            ActualOutput = result?.ActualOutput,
                            ErrorMessage = result?.ErrorMessage,
                            ExecutionTimeMs = result?.ExecutionTimeMs,
                        });
                    }

                    _context.AssignmentTestResults.AddRange(testResults);
                    foreach (var testResult in testResults)
                    {
                        submission.TestResults.Add(testResult);
                    }

                    submission.AutoScore = AssignmentGradingCalculator.CalculateAutoScore(outcomes);
                    submission.AutoGradingStatus = AssignmentGradingStatuses.Completed;
                }
                catch (Exception)
                {
                    submission.AutoGradingStatus = AssignmentGradingStatuses.Failed;
                }
            }
            else
            {
                submission.AutoGradingStatus = AssignmentGradingStatuses.Completed;
            }

            submission.FinalScore = submission.ManualScore ?? submission.AutoScore;

            _context.AssignmentSubmissions.Add(submission);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "assignment.submitted", nameof(AssignmentSubmission), submission.Id,
                new { assignmentId = assignment.Id, isLate }));

            await _context.SaveChangesAsync(cancellationToken);

            return SubmissionResultMapping.ToDto(submission);
        }

        private Guid GetCurrentUserId()
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated user could not be resolved.");
            }

            return userId;
        }
    }
}
