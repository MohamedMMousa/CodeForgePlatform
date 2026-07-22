using CodeForge.Application.Users.CreateInstructor;
using FluentValidation.TestHelper;
using Xunit;

namespace CodeForge.UnitTests.Users
{
    public class CreateInstructorCommandValidatorTests
    {
        private readonly CreateInstructorCommandValidator _validator = new();

        private static CreateInstructorCommand Valid() => new(
            FullName: "Ada Lovelace",
            Email: "ada@example.com",
            Phone: "+201000000000");

        [Fact]
        public void Validate_ValidCommand_HasNoErrors()
        {
            var result = _validator.TestValidate(Valid());

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyFullName_HasError()
        {
            var command = Valid() with { FullName = "" };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.FullName);
        }

        [Fact]
        public void Validate_EmptyEmail_HasError()
        {
            var command = Valid() with { Email = "" };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_InvalidEmail_HasError()
        {
            var command = Valid() with { Email = "not-an-email" };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_NullPhone_HasNoErrors()
        {
            var command = Valid() with { Phone = null };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_PhoneTooLong_HasError()
        {
            var command = Valid() with { Phone = new string('1', 31) };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Phone);
        }
    }
}
