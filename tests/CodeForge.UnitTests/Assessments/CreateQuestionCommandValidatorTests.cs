using CodeForge.Application.Assessments.Common;
using CodeForge.Application.Assessments.CreateQuestion;
using FluentValidation.TestHelper;
using Xunit;

namespace CodeForge.UnitTests.Assessments
{
    public class CreateQuestionCommandValidatorTests
    {
        private readonly CreateQuestionCommandValidator _validator = new();

        private static List<OptionInputDto> TwoOptionsOneCorrect() => new()
        {
            new OptionInputDto("Paris", true),
            new OptionInputDto("London", false),
        };

        [Fact]
        public void Validate_ExactlyOneCorrectOption_HasNoErrors()
        {
            var command = new CreateQuestionCommand(Guid.NewGuid(), "What is the capital of France?", TwoOptionsOneCorrect());

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_NoCorrectOption_HasError()
        {
            var options = new List<OptionInputDto>
            {
                new("Paris", false),
                new("London", false),
            };
            var command = new CreateQuestionCommand(Guid.NewGuid(), "What is the capital of France?", options);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Options);
        }

        [Fact]
        public void Validate_MultipleCorrectOptions_HasError()
        {
            var options = new List<OptionInputDto>
            {
                new("Paris", true),
                new("London", true),
            };
            var command = new CreateQuestionCommand(Guid.NewGuid(), "What is the capital of France?", options);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Options);
        }

        [Fact]
        public void Validate_OnlyOneOption_HasError()
        {
            var options = new List<OptionInputDto> { new("Paris", true) };
            var command = new CreateQuestionCommand(Guid.NewGuid(), "What is the capital of France?", options);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Options);
        }

        [Fact]
        public void Validate_EmptyQuestionText_HasError()
        {
            var command = new CreateQuestionCommand(Guid.NewGuid(), "", TwoOptionsOneCorrect());

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionText);
        }
    }
}
