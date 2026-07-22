namespace CodeForge.Application.Common.Interfaces
{
    public record TestCaseExecutionInput(Guid TestCaseId, string Input, string ExpectedOutput);

    public record TestCaseExecutionResult(
        Guid TestCaseId,
        bool Passed,
        string? ActualOutput,
        string? ErrorMessage,
        int? ExecutionTimeMs);

    public interface ICodeExecutionService
    {
        Task<IReadOnlyList<TestCaseExecutionResult>> RunAsync(
            string code,
            string language,
            IReadOnlyList<TestCaseExecutionInput> testCases,
            CancellationToken cancellationToken = default);
    }
}
