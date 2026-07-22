namespace CodeForge.Application.Assessments.Common
{
    public record QuestionDto(Guid Id, string QuestionText, int OrderIndex, List<OptionDto> Options);
}
