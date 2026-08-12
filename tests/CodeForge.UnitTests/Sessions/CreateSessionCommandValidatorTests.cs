using CodeForge.Application.Common.Constants;
using CodeForge.Application.Sessions.CreateSession;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CodeForge.UnitTests.Sessions
{
    public class CreateSessionCommandValidatorTests
    {
        private readonly CreateSessionCommandValidator _validator = new();

        private static readonly DateTime ScheduledUtc = new(2026, 8, 20, 18, 0, 0, DateTimeKind.Utc);

        private static CreateSessionCommand ValidLive() => new(
            ModuleId: Guid.NewGuid(),
            Type: SessionTypes.Live,
            Title: "Intro to Async",
            Description: null,
            ScheduledAt: ScheduledUtc,
            DurationMinutes: 90,
            JoinLink: "https://meet.example.com/abc",
            Location: null,
            VideoUrl: null,
            InstructorId: null);

        [Fact]
        public void Validate_ValidLiveSession_HasNoErrors()
        {
            var result = _validator.TestValidate(ValidLive());

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_LiveSessionWithoutScheduledAt_HasError()
        {
            var command = ValidLive() with { ScheduledAt = null };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.ScheduledAt);
        }

        [Fact]
        public void Validate_LiveSessionWithoutJoinLink_HasError()
        {
            var command = ValidLive() with { JoinLink = null };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.JoinLink);
        }

        [Fact]
        public void Validate_InPersonWithoutLocation_HasError()
        {
            var command = ValidLive() with
            {
                Type = SessionTypes.InPerson,
                JoinLink = null,
                Location = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Location);
        }

        [Fact]
        public void Validate_RecordedLessonWithoutVideoUrl_HasError()
        {
            var command = ValidLive() with
            {
                Type = SessionTypes.RecordedLesson,
                ScheduledAt = null,
                JoinLink = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.VideoUrl);
        }

        [Fact]
        public void Validate_UnknownType_HasError()
        {
            var command = ValidLive() with { Type = "webinar" };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Type);
        }

        // A timezone-less ScheduledAt used to reach Npgsql, which rejects any non-UTC
        // DateTime for a `timestamptz` column — surfacing as an opaque 500 rather than a
        // validation failure. It has to fail here, on the field, with a mappable code.
        [Fact]
        public void Validate_ScheduledAtWithoutTimeZone_HasError()
        {
            var command = ValidLive() with
            {
                ScheduledAt = new DateTime(2026, 8, 20, 18, 0, 0, DateTimeKind.Unspecified)
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.ScheduledAt)
                .WithErrorCode(ValidationErrorCodes.TimestampNotUtc);
        }

        [Fact]
        public void Validate_ScheduledAtInUtc_HasNoScheduledAtError()
        {
            var result = _validator.TestValidate(ValidLive());

            result.ShouldNotHaveValidationErrorFor(x => x.ScheduledAt);
        }

        [Fact]
        public void Validate_NullScheduledAt_IsNotRejectedByTheUtcRule()
        {
            // A recorded lesson legitimately has no scheduled time; the UTC rule must not
            // fire on absence.
            var command = ValidLive() with
            {
                Type = SessionTypes.RecordedLesson,
                ScheduledAt = null,
                JoinLink = null,
                VideoUrl = "https://videos.example.com/lesson-1"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ZeroDuration_HasError()
        {
            var command = ValidLive() with { DurationMinutes = 0 };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.DurationMinutes);
        }
    }
}
