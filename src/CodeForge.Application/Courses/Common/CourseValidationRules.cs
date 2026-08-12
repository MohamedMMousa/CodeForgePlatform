using CodeForge.Application.Common.Constants;
using FluentValidation;
using FluentValidation.Results;

namespace CodeForge.Application.Courses.Common
{
    public static class CourseValidationRules
    {
        public static readonly string[] ValidStatuses =
        {
            CourseStatuses.Draft,
            CourseStatuses.Published,
            CourseStatuses.Archived
        };

        public static bool IsValidSlug(string slug)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(
                slug,
                "^[a-z0-9]+(?:-[a-z0-9]+)*$");
        }

        /// <summary>
        /// Uniqueness needs a database round-trip, so it is checked in the handler rather than
        /// in a validator — but the failure is still an input problem about a specific field.
        /// Reporting it as a <see cref="ValidationException"/> puts it in the same envelope as
        /// the slug-format failure, so the frontend can render both inline on the slug input
        /// instead of one inline and one as a detached banner.
        /// </summary>
        public static ValidationException SlugTakenException(string message)
        {
            return new ValidationException(new[]
            {
                new ValidationFailure("Slug", message)
                {
                    ErrorCode = ValidationErrorCodes.SlugTaken
                }
            });
        }
    }
}
