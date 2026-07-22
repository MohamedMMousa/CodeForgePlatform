using CodeForge.Application.Assessments.CreateAssessment;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CodeForge.UnitTests.Assessments
{
    public class CreateAssessmentCommandValidatorTests
    {
        private readonly CreateAssessmentCommandValidator _validator = new();

        private static CreateAssessmentCommand ValidQuiz() => new(
            ModuleId: Guid.NewGuid(),
            Type: "quiz",
            Title: "Chapter 1 Quiz",
            TimeLimitMinutes: 20,
            PassScore: 70,
            IsPractice: false,
            MaxAttempts: 3,
            RandomizeQuestions: false,
            DisableCopyPaste: false);

        [Fact]
        public void Validate_ValidQuiz_HasNoErrors()
        {
            var result = _validator.TestValidate(ValidQuiz());

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_UnknownType_HasError()
        {
            var command = ValidQuiz() with { Type = "midterm" };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Type);
        }

        [Fact]
        public void Validate_ExamWithMultipleAttempts_HasError()
        {
            var command = ValidQuiz() with { Type = "exam", MaxAttempts = 3 };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.MaxAttempts);
        }

        [Fact]
        public void Validate_ExamWithSingleAttempt_HasNoErrors()
        {
            var command = ValidQuiz() with { Type = "exam", MaxAttempts = 1 };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_PassScoreOutOfRange_HasError()
        {
            var command = ValidQuiz() with { PassScore = 150 };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.PassScore);
        }

        [Fact]
        public void Validate_EmptyTitle_HasError()
        {
            var command = ValidQuiz() with { Title = "" };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Title);
        }
    }
}
