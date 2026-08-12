using CodeForge.Application.Common.Constants;
using FluentValidation;

namespace CodeForge.Application.Common.Validation
{
    /// <summary>
    /// Shared FluentValidation rules for timestamps.
    /// </summary>
    public static class DateTimeRules
    {
        private const string AmbiguousTimestampMessage =
            "Timestamp must include a time zone — send UTC (e.g. 2026-08-20T18:00:00Z) or an explicit offset.";

        /// <summary>
        /// Rejects a timestamp that carries no time zone.
        /// <para>
        /// Every timestamp column is <c>timestamp with time zone</c>, and Npgsql will only write
        /// a <see cref="DateTimeKind.Utc"/> value to one — anything else throws inside
        /// <c>SaveChangesAsync</c> and surfaces as an unactionable 500.
        /// <c>UtcDateTimeConverter</c> has already folded values with an explicit offset into
        /// UTC by the time validation runs, so a <see cref="DateTimeKind.Unspecified"/> value
        /// here means the client genuinely sent no zone. That is unrecoverable — this rule fails
        /// it as a 400 naming the field rather than letting it become a 500.
        /// </para>
        /// </summary>
        public static IRuleBuilderOptions<T, DateTime> MustBeUtc<T>(
            this IRuleBuilder<T, DateTime> ruleBuilder)
            => ruleBuilder
                .Must(value => value.Kind != DateTimeKind.Unspecified)
                .WithErrorCode(ValidationErrorCodes.TimestampNotUtc)
                .WithMessage(AmbiguousTimestampMessage);

        /// <inheritdoc cref="MustBeUtc{T}(IRuleBuilder{T, DateTime})"/>
        public static IRuleBuilderOptions<T, DateTime?> MustBeUtc<T>(
            this IRuleBuilder<T, DateTime?> ruleBuilder)
            => ruleBuilder
                .Must(value => !value.HasValue || value.Value.Kind != DateTimeKind.Unspecified)
                .WithErrorCode(ValidationErrorCodes.TimestampNotUtc)
                .WithMessage(AmbiguousTimestampMessage);
    }
}
