using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeForge.Api.Serialization
{
    /// <summary>
    /// Normalizes incoming <see cref="DateTime"/> values to UTC at the JSON boundary.
    /// <para>
    /// Every timestamp column in the schema is <c>timestamp with time zone</c>, and Npgsql
    /// refuses to write a <see cref="DateTime"/> whose <see cref="DateTime.Kind"/> is not
    /// <see cref="DateTimeKind.Utc"/> — it throws <see cref="ArgumentException"/> deep inside
    /// <c>SaveChangesAsync</c>, which the exception middleware can only render as a 500.
    /// </para>
    /// <para>
    /// A payload carrying an explicit offset (<c>2026-08-20T18:00:00+03:00</c>) parses as
    /// <see cref="DateTimeKind.Local"/>. The offset pins the instant exactly, so converting it
    /// is lossless and clients are not forced to pre-convert. A payload with no zone at all
    /// (<c>2026-08-20T18:00</c>) parses as <see cref="DateTimeKind.Unspecified"/> and is passed
    /// through untouched: the intended zone is unrecoverable here, so guessing would silently
    /// store a time hours adrift. <c>DateTimeRules.MustBeUtc()</c> rejects those in validation,
    /// turning an ambiguous timestamp into a 400 on the offending field instead of a 500.
    /// </para>
    /// </summary>
    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Normalize(reader.GetDateTime());

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(Normalize(value));

        internal static DateTime Normalize(DateTime value) => value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => value
        };
    }

    /// <inheritdoc cref="UtcDateTimeConverter"/>
    public class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType == JsonTokenType.Null
                ? null
                : UtcDateTimeConverter.Normalize(reader.GetDateTime());

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(UtcDateTimeConverter.Normalize(value.Value));
        }
    }
}
