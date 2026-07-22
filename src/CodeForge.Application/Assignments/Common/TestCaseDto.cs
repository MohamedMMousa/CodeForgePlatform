namespace CodeForge.Application.Assignments.Common
{
    public record TestCaseInputDto(string Input, string ExpectedOutput, bool IsHidden, int Points);

    public record TestCaseDto(Guid Id, string Input, string ExpectedOutput, bool IsHidden, int Points, int OrderIndex);
}
