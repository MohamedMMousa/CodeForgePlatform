using CodeForge.Application.Common.Constants;

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
    }
}
