using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CodeForge.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CodeForge.Infrastructure.Assessments
{
    /// <summary>
    /// Runs student code against instructor-defined test cases via Piston
    /// (emkc.org/api/v2/piston) — a free, public, sandboxed code-execution API. No
    /// Docker/local sandboxing required; chosen because this environment has no
    /// Docker installed. Isolated behind ICodeExecutionService so it can be swapped
    /// for a self-hosted Judge0/Piston runner once hosting is decided (Phase 5).
    /// </summary>
    public class PistonCodeExecutionService : ICodeExecutionService
    {
        private const string ExecuteEndpoint = "https://emkc.org/api/v2/piston/execute";
        private static readonly Dictionary<string, (string Language, string Version)> LanguageMap = new()
        {
            ["python"] = ("python", "3.10.0"),
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger<PistonCodeExecutionService> _logger;

        public PistonCodeExecutionService(HttpClient httpClient, ILogger<PistonCodeExecutionService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IReadOnlyList<TestCaseExecutionResult>> RunAsync(
            string code,
            string language,
            IReadOnlyList<TestCaseExecutionInput> testCases,
            CancellationToken cancellationToken = default)
        {
            if (!LanguageMap.TryGetValue(language, out var runtime))
            {
                throw new InvalidOperationException($"Unsupported auto-grader language '{language}'.");
            }

            var results = new List<TestCaseExecutionResult>(testCases.Count);
            foreach (var testCase in testCases)
            {
                results.Add(await RunOneAsync(code, runtime.Language, runtime.Version, testCase, cancellationToken));
            }
            return results;
        }

        private async Task<TestCaseExecutionResult> RunOneAsync(
            string code,
            string language,
            string version,
            TestCaseExecutionInput testCase,
            CancellationToken cancellationToken)
        {
            var startedAt = DateTime.UtcNow;
            try
            {
                var request = new PistonExecuteRequest(
                    language,
                    version,
                    new[] { new PistonFile("main.py", code) },
                    testCase.Input);

                using var response = await _httpClient.PostAsJsonAsync(ExecuteEndpoint, request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadFromJsonAsync<PistonExecuteResponse>(cancellationToken: cancellationToken);
                var elapsedMs = (int)(DateTime.UtcNow - startedAt).TotalMilliseconds;

                if (body?.Run is null)
                {
                    return new TestCaseExecutionResult(testCase.TestCaseId, false, null, "Piston returned no run output.", elapsedMs);
                }

                var actualOutput = body.Run.Stdout?.TrimEnd('\n', '\r') ?? string.Empty;
                var expectedOutput = testCase.ExpectedOutput.TrimEnd('\n', '\r');
                var passed = body.Run.Code == 0 && actualOutput == expectedOutput;
                var errorMessage = body.Run.Code != 0 ? body.Run.Stderr : null;

                return new TestCaseExecutionResult(testCase.TestCaseId, passed, body.Run.Stdout, errorMessage, elapsedMs);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Piston execution failed for test case {TestCaseId}.", testCase.TestCaseId);
                var elapsedMs = (int)(DateTime.UtcNow - startedAt).TotalMilliseconds;
                return new TestCaseExecutionResult(testCase.TestCaseId, false, null, "Auto-grader execution failed.", elapsedMs);
            }
        }

        private record PistonExecuteRequest(
            [property: JsonPropertyName("language")] string Language,
            [property: JsonPropertyName("version")] string Version,
            [property: JsonPropertyName("files")] PistonFile[] Files,
            [property: JsonPropertyName("stdin")] string Stdin);

        private record PistonFile(
            [property: JsonPropertyName("name")] string Name,
            [property: JsonPropertyName("content")] string Content);

        private record PistonExecuteResponse(
            [property: JsonPropertyName("run")] PistonRunResult? Run);

        private record PistonRunResult(
            [property: JsonPropertyName("stdout")] string? Stdout,
            [property: JsonPropertyName("stderr")] string? Stderr,
            [property: JsonPropertyName("code")] int Code);
    }
}
