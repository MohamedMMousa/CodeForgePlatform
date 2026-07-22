using CodeForge.Application.Common.Interfaces;

namespace CodeForge.Infrastructure.Assessments
{
    /// <summary>
    /// No auto-grader engine is currently reachable from this environment: Piston's
    /// public API went whitelist-only on 2026-02-15 (confirmed via a direct 401
    /// response), and no Docker/self-hosted alternative is available here. Every
    /// submission is therefore left for 100% manual grading, which SRS §7 already
    /// requires as a first-class path ("instructor fully controls every assessment") —
    /// this is a functional fallback, not a broken feature. PistonCodeExecutionService
    /// is kept intact and ready to swap back in (in DependencyInjection.cs) once
    /// whitelisted, or replace with a self-hosted engine once hosting is decided
    /// (Phase 5).
    /// </summary>
    public class DeferredCodeExecutionService : ICodeExecutionService
    {
        public Task<IReadOnlyList<TestCaseExecutionResult>> RunAsync(
            string code,
            string language,
            IReadOnlyList<TestCaseExecutionInput> testCases,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("No auto-grader engine is currently configured; submissions require manual grading.");
        }
    }
}
