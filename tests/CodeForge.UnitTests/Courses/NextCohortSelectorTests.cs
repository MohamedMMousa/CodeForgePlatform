using CodeForge.Application.Common.Constants;
using CodeForge.Application.Courses.Common;
using FluentAssertions;
using Xunit;

namespace CodeForge.UnitTests.Courses
{
    public class NextCohortSelectorTests
    {
        private static readonly DateTime Now = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        private static NextCohortSelector.Candidate Candidate(
            int seed,
            DateTime startDate,
            DateTime cutoffDate,
            int capacity,
            int enrolledCount)
        {
            return new NextCohortSelector.Candidate(
                CohortId: new Guid($"00000000-0000-0000-0000-{seed:D12}"),
                Name: $"Batch {seed}",
                StartDate: startDate,
                EnrollmentCutoffDate: cutoffDate,
                Capacity: capacity,
                EnrolledCount: enrolledCount);
        }

        [Fact]
        public void Select_PlentyOfSeats_ReturnsOpen()
        {
            var candidates = new[]
            {
                Candidate(1, Now.AddDays(10), Now.AddDays(5), capacity: 20, enrolledCount: 5)
            };

            var result = NextCohortSelector.Select(candidates, Now);

            result.Should().NotBeNull();
            result!.Status.Should().Be(NextCohortStatuses.Open);
            result.SeatsLeft.Should().Be(15);
        }

        [Fact]
        public void Select_SeatsAtThreshold_ReturnsAlmostFull()
        {
            // capacity 20, enrolled 17 -> seatsLeft 3 == AlmostFullSeatsThreshold
            var candidates = new[]
            {
                Candidate(1, Now.AddDays(10), Now.AddDays(5), capacity: 20, enrolledCount: 17)
            };

            var result = NextCohortSelector.Select(candidates, Now);

            result!.SeatsLeft.Should().Be(3);
            result.Status.Should().Be(NextCohortStatuses.AlmostFull);
        }

        [Fact]
        public void Select_SeatsJustAboveThreshold_ReturnsOpen()
        {
            // capacity 20, enrolled 16 -> seatsLeft 4, one above the threshold
            var candidates = new[]
            {
                Candidate(1, Now.AddDays(10), Now.AddDays(5), capacity: 20, enrolledCount: 16)
            };

            var result = NextCohortSelector.Select(candidates, Now);

            result!.SeatsLeft.Should().Be(4);
            result.Status.Should().Be(NextCohortStatuses.Open);
        }

        [Fact]
        public void Select_NoCandidates_ReturnsNull()
        {
            var result = NextCohortSelector.Select(Array.Empty<NextCohortSelector.Candidate>(), Now);

            result.Should().BeNull();
        }

        [Fact]
        public void Select_CutoffAlreadyPassed_ReturnsNull()
        {
            var candidates = new[]
            {
                Candidate(1, Now.AddDays(10), Now.AddDays(-1), capacity: 20, enrolledCount: 5)
            };

            var result = NextCohortSelector.Select(candidates, Now);

            result.Should().BeNull();
        }

        [Fact]
        public void Select_AtCapacity_ReturnsNull_AndNeverNegativeSeats()
        {
            var candidates = new[]
            {
                Candidate(1, Now.AddDays(10), Now.AddDays(5), capacity: 20, enrolledCount: 20),
                Candidate(2, Now.AddDays(20), Now.AddDays(15), capacity: 5, enrolledCount: 7)
            };

            var result = NextCohortSelector.Select(candidates, Now);

            result.Should().BeNull();
        }

        [Fact]
        public void Select_TwoBookableCohorts_EarliestStartDateWins()
        {
            var later = Candidate(1, Now.AddDays(30), Now.AddDays(25), capacity: 20, enrolledCount: 5);
            var earlier = Candidate(2, Now.AddDays(10), Now.AddDays(5), capacity: 20, enrolledCount: 5);

            var result = NextCohortSelector.Select(new[] { later, earlier }, Now);

            result!.CohortId.Should().Be(earlier.CohortId);
        }

        [Fact]
        public void Select_EqualStartDates_TieBreaksByCohortId()
        {
            var sameDate = Now.AddDays(10);
            var higherId = Candidate(9, sameDate, Now.AddDays(5), capacity: 20, enrolledCount: 5);
            var lowerId = Candidate(1, sameDate, Now.AddDays(5), capacity: 20, enrolledCount: 5);

            var result = NextCohortSelector.Select(new[] { higherId, lowerId }, Now);

            result!.CohortId.Should().Be(lowerId.CohortId);
        }

        [Fact]
        public void Select_EarlierCohortFull_SkipsToLaterBookableCohort()
        {
            var fullEarlier = Candidate(1, Now.AddDays(10), Now.AddDays(5), capacity: 20, enrolledCount: 20);
            var bookableLater = Candidate(2, Now.AddDays(20), Now.AddDays(15), capacity: 20, enrolledCount: 5);

            var result = NextCohortSelector.Select(new[] { fullEarlier, bookableLater }, Now);

            result!.CohortId.Should().Be(bookableLater.CohortId);
            result.Status.Should().Be(NextCohortStatuses.Open);
        }
    }
}
