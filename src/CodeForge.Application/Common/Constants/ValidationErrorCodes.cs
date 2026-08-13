namespace CodeForge.Application.Common.Constants
{
    /// <summary>
    /// Stable, machine-readable codes attached to validation failures via
    /// <c>.WithErrorCode(...)</c>. They ride alongside the English messages in the
    /// <c>errorCodes</c> dictionary of the validation envelope (see API_CONVENTIONS.md §4)
    /// so the frontend can render its own bilingual copy instead of the server's English.
    /// Only rules whose FluentValidation default code is ambiguous (e.g. every
    /// <c>.Must(...)</c> reports "PredicateValidator") need an entry here.
    /// </summary>
    public static class ValidationErrorCodes
    {
        public const string SlugFormat = "slug_format";
        public const string SlugTaken = "slug_taken";
        public const string TimestampNotUtc = "timestamp_not_utc";
        public const string InvalidUrl = "invalid_url";
    }
}
