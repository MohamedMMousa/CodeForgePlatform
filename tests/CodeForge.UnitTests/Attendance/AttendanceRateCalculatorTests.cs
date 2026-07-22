using CodeForge.Application.Attendance.Common;
using CodeForge.Application.Common.Constants;
using FluentAssertions;
using Xunit;

namespace CodeForge.UnitTests.Attendance
{
    public class AttendanceRateCalculatorTests
    {
        [Fact]
        public void Calculate_AllPresent_Returns100PercentRate()
        {
            var statuses = new[] { AttendanceStatuses.Present, AttendanceStatuses.Present };

            var result = AttendanceRateCalculator.Calculate(heldSessionsCount: 2, recordedStatuses: statuses);

            result.Rate.Should().Be(100m);
        }

        [Fact]
        public void Calculate_LateCountsAsPresent()
        {
            var statuses = new[] { AttendanceStatuses.Late };

            var result = AttendanceRateCalculator.Calculate(heldSessionsCount: 1, recordedStatuses: statuses);

            result.Rate.Should().Be(100m);
        }

        [Fact]
        public void Calculate_ExcusedSession_ExcludedFromDenominator()
        {
            var statuses = new[] { AttendanceStatuses.Excused };

            var result = AttendanceRateCalculator.Calculate(heldSessionsCount: 1, recordedStatuses: statuses);

            result.EffectiveHeld.Should().Be(0);
            result.Rate.Should().Be(0m);
        }

        [Fact]
        public void Calculate_UnmarkedSession_CountsAsAbsent()
        {
            var result = AttendanceRateCalculator.Calculate(heldSessionsCount: 2, recordedStatuses: Array.Empty<string>());

            result.PresentCount.Should().Be(0);
            result.Rate.Should().Be(0m);
        }

        [Fact]
        public void Calculate_NoSessionsHeld_ReturnsZeroRate_NotDivideByZero()
        {
            var result = AttendanceRateCalculator.Calculate(heldSessionsCount: 0, recordedStatuses: Array.Empty<string>());

            result.Rate.Should().Be(0m);
        }
    }
}
