using System.Text.Json;
using CodeForge.Api.Serialization;
using FluentAssertions;
using Xunit;

namespace CodeForge.UnitTests.Serialization
{
    /// <summary>
    /// The converters exist so a timestamp that unambiguously identifies an instant reaches
    /// EF as UTC — Npgsql rejects anything else for a <c>timestamptz</c> column, and that
    /// rejection can only surface as a 500. Anything genuinely ambiguous must survive to
    /// validation instead, where it becomes a 400 naming the field.
    /// </summary>
    public class UtcDateTimeConverterTests
    {
        private static readonly JsonSerializerOptions Options = BuildOptions();

        private static JsonSerializerOptions BuildOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new UtcDateTimeConverter());
            options.Converters.Add(new NullableUtcDateTimeConverter());
            return options;
        }

        private record Payload(DateTime? ScheduledAt);

        private record RequiredPayload(DateTime StartDate);

        private static DateTime? Deserialize(string value)
            => JsonSerializer.Deserialize<Payload>($"{{\"scheduledAt\":{value}}}", Options)!.ScheduledAt;

        [Fact]
        public void Read_ExplicitUtc_StaysUtc()
        {
            var result = Deserialize("\"2026-08-20T18:00:00Z\"");

            result!.Value.Kind.Should().Be(DateTimeKind.Utc);
            result.Value.Should().Be(new DateTime(2026, 8, 20, 18, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void Read_ExplicitOffset_NormalizesToTheSameInstantInUtc()
        {
            // +03:00 pins the instant exactly, so converting is lossless — 18:00+03:00 is 15:00Z.
            var result = Deserialize("\"2026-08-20T18:00:00+03:00\"");

            result!.Value.Kind.Should().Be(DateTimeKind.Utc);
            result.Value.Should().Be(new DateTime(2026, 8, 20, 15, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void Read_NoTimeZone_StaysUnspecifiedForValidationToReject()
        {
            // This is what `<input type="datetime-local">` sends. The intended zone is
            // unrecoverable here, so the converter must not guess — MustBeUtc() fails it.
            var result = Deserialize("\"2026-08-20T18:00\"");

            result!.Value.Kind.Should().Be(DateTimeKind.Unspecified);
        }

        [Fact]
        public void Read_Null_StaysNull()
        {
            Deserialize("null").Should().BeNull();
        }

        [Fact]
        public void Read_NonNullableProperty_NormalizesOffsetToUtc()
        {
            var result = JsonSerializer.Deserialize<RequiredPayload>(
                "{\"startDate\":\"2026-08-20T18:00:00+03:00\"}", Options)!;

            result.StartDate.Kind.Should().Be(DateTimeKind.Utc);
            result.StartDate.Should().Be(new DateTime(2026, 8, 20, 15, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void Write_UtcValue_SerializesWithZuluSuffix()
        {
            var json = JsonSerializer.Serialize(
                new Payload(new DateTime(2026, 8, 20, 18, 0, 0, DateTimeKind.Utc)), Options);

            json.Should().Contain("2026-08-20T18:00:00Z");
        }

        [Fact]
        public void Write_Null_SerializesAsNull()
        {
            var json = JsonSerializer.Serialize(new Payload(null), Options);

            json.Should().Contain("null");
        }
    }
}
