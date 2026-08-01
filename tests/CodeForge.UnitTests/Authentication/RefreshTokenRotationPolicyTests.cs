using CodeForge.Application.Authentication.RefreshToken;
using FluentAssertions;
using Xunit;

namespace CodeForge.UnitTests.Authentication
{
    public class RefreshTokenRotationPolicyTests
    {
        private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Rotate_WhenPresentedTokenMatchesCurrent()
        {
            var outcome = RefreshTokenRotationPolicy.Decide(
                currentHash: "current-hash",
                previousHash: "previous-hash",
                rotatedAt: Now.AddMinutes(-10),
                presentedHash: "current-hash",
                now: Now);

            outcome.Should().Be(RefreshTokenRotationOutcome.Rotate);
        }

        [Fact]
        public void Rotate_WhenNoPriorRotationRecorded_AndPresentedMatchesCurrent()
        {
            var outcome = RefreshTokenRotationPolicy.Decide(
                currentHash: "current-hash",
                previousHash: null,
                rotatedAt: null,
                presentedHash: "current-hash",
                now: Now);

            outcome.Should().Be(RefreshTokenRotationOutcome.Rotate);
        }

        [Fact]
        public void ReturnCurrent_WhenPresentedMatchesPrevious_WithinGraceWindow()
        {
            var outcome = RefreshTokenRotationPolicy.Decide(
                currentHash: "current-hash",
                previousHash: "previous-hash",
                rotatedAt: Now.AddSeconds(-15),
                presentedHash: "previous-hash",
                now: Now);

            outcome.Should().Be(RefreshTokenRotationOutcome.ReturnCurrent);
        }

        [Fact]
        public void ReturnCurrent_AtExactGraceWindowBoundary_Inclusive()
        {
            var outcome = RefreshTokenRotationPolicy.Decide(
                currentHash: "current-hash",
                previousHash: "previous-hash",
                rotatedAt: Now - RefreshTokenRotationPolicy.GraceWindow,
                presentedHash: "previous-hash",
                now: Now);

            outcome.Should().Be(RefreshTokenRotationOutcome.ReturnCurrent);
        }

        [Fact]
        public void Reuse_WhenPresentedMatchesPrevious_JustOutsideGraceWindow()
        {
            var outcome = RefreshTokenRotationPolicy.Decide(
                currentHash: "current-hash",
                previousHash: "previous-hash",
                rotatedAt: Now - RefreshTokenRotationPolicy.GraceWindow - TimeSpan.FromSeconds(1),
                presentedHash: "previous-hash",
                now: Now);

            outcome.Should().Be(RefreshTokenRotationOutcome.Reuse);
        }

        [Fact]
        public void Reuse_WhenPresentedMatchesPrevious_ButRotatedAtIsMissing()
        {
            var outcome = RefreshTokenRotationPolicy.Decide(
                currentHash: "current-hash",
                previousHash: "previous-hash",
                rotatedAt: null,
                presentedHash: "previous-hash",
                now: Now);

            outcome.Should().Be(RefreshTokenRotationOutcome.Reuse);
        }

        [Fact]
        public void Invalid_WhenPresentedMatchesNeitherCurrentNorPrevious()
        {
            var outcome = RefreshTokenRotationPolicy.Decide(
                currentHash: "current-hash",
                previousHash: "previous-hash",
                rotatedAt: Now.AddSeconds(-5),
                presentedHash: "some-other-hash",
                now: Now);

            outcome.Should().Be(RefreshTokenRotationOutcome.Invalid);
        }

        [Fact]
        public void Invalid_WhenNoPreviousRecorded_AndPresentedDoesNotMatchCurrent()
        {
            var outcome = RefreshTokenRotationPolicy.Decide(
                currentHash: "current-hash",
                previousHash: null,
                rotatedAt: null,
                presentedHash: "some-other-hash",
                now: Now);

            outcome.Should().Be(RefreshTokenRotationOutcome.Invalid);
        }

        [Fact]
        public void CurrentHashMatch_TakesPriorityOver_PreviousHashMatch()
        {
            // Degenerate case (current and previous happen to be equal); current wins.
            var outcome = RefreshTokenRotationPolicy.Decide(
                currentHash: "same-hash",
                previousHash: "same-hash",
                rotatedAt: Now - RefreshTokenRotationPolicy.GraceWindow - TimeSpan.FromMinutes(1),
                presentedHash: "same-hash",
                now: Now);

            outcome.Should().Be(RefreshTokenRotationOutcome.Rotate);
        }
    }
}
