using CodeForge.Application.Courses.GetAssignedCourses;
using CodeForge.Application.Courses.GetCourses;
using CodeForge.Application.Users.GetUsers;
using FluentValidation.TestHelper;
using Xunit;

namespace CodeForge.UnitTests.Common
{
    /// <summary>
    /// Page/PageSize bounds are the same hand-written rule pair repeated across all 12
    /// paginated list validators (see API_CONVENTIONS.md §6). Exercised here against one
    /// validator per handler shape touched by the pagination change, rather than all 12.
    /// </summary>
    public class PaginationValidatorTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_PageLessThanOne_HasError(int page)
        {
            var validator = new GetCoursesQueryValidator();
            var query = new GetCoursesQuery(null, null, null, page, 20);

            var result = validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.Page);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void Validate_PageSizeOutsideBounds_HasError(int pageSize)
        {
            var validator = new GetUsersQueryValidator();
            var query = new GetUsersQuery(null, null, null, 1, pageSize);

            var result = validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.PageSize);
        }

        [Fact]
        public void Validate_DefaultPageAndPageSize_HasNoErrors()
        {
            var validator = new GetAssignedCoursesQueryValidator();
            var query = new GetAssignedCoursesQuery();

            var result = validator.TestValidate(query);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_MaxPageSize_HasNoErrors()
        {
            var validator = new GetUsersQueryValidator();
            var query = new GetUsersQuery(null, null, null, 1, 100);

            var result = validator.TestValidate(query);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
