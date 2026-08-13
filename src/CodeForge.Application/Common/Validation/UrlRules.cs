using CodeForge.Application.Common.Constants;
using FluentValidation;

namespace CodeForge.Application.Common.Validation
{
    /// <summary>
    /// Shared FluentValidation rules and save-time normalization for URL fields (join links,
    /// video URLs) that instructors often type without a scheme, e.g. "zoom.com" instead of
    /// "https://zoom.com". Left as-is, a scheme-less value becomes a relative link on the
    /// frontend and resolves against our own origin instead of the external site.
    /// </summary>
    public static class UrlRules
    {
        private const string InvalidUrlMessage = "Enter a valid URL, e.g. https://zoom.us/j/123.";

        /// <summary>
        /// Accepts an absolute http/https URL as-is; if the value has no scheme, retries with
        /// "https://" prepended so plain domains like "zoom.com" are recognized.
        /// </summary>
        public static bool TryNormalize(string raw, out string normalized)
        {
            var trimmed = raw.Trim();

            if (IsAbsoluteHttpUrl(trimmed, out var uri) || IsAbsoluteHttpUrl($"https://{trimmed}", out uri))
            {
                normalized = uri.ToString();
                return true;
            }

            normalized = trimmed;
            return false;
        }

        /// <summary>
        /// Null/whitespace in, null out. Otherwise the normalized form, or the trimmed raw
        /// value if it can't be normalized — the validator is expected to have already
        /// rejected that case, so this fallback only matters for optional, unvalidated inputs.
        /// </summary>
        public static string? NormalizeOrNull(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            return TryNormalize(raw, out var normalized) ? normalized : raw.Trim();
        }

        public static IRuleBuilderOptions<T, string?> MustBeNormalizableHttpUrl<T>(
            this IRuleBuilder<T, string?> ruleBuilder)
            => ruleBuilder
                .Must(value => string.IsNullOrWhiteSpace(value) || TryNormalize(value, out _))
                .WithErrorCode(ValidationErrorCodes.InvalidUrl)
                .WithMessage(InvalidUrlMessage);

        private static bool IsAbsoluteHttpUrl(string value, out Uri uri)
        {
            var ok = Uri.TryCreate(value, UriKind.Absolute, out var parsed)
                && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
            uri = ok ? parsed! : null!;
            return ok;
        }
    }
}
