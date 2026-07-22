using CodeForge.Application.Analytics.Common;
using FluentAssertions;
using Xunit;

namespace CodeForge.UnitTests.Analytics
{
    public class AnalyticsCalculatorTests
    {
        [Fact]
        public void PassRate_NoSubmissions_ReturnsZero_NotDivideByZero()
        {
            AnalyticsCalculator.PassRate(submittedCount: 0, passedCount: 0).Should().Be(0m);
        }

        [Fact]
        public void PassRate_AllPassed_Returns100()
        {
            AnalyticsCalculator.PassRate(submittedCount: 4, passedCount: 4).Should().Be(100m);
        }

        [Fact]
        public void PassRate_HalfPassed_Returns50()
        {
            AnalyticsCalculator.PassRate(submittedCount: 4, passedCount: 2).Should().Be(50m);
        }

        [Fact]
        public void PassRate_RoundsToOneDecimal()
        {
            // 1/3 = 33.33% → 33.3
            AnalyticsCalculator.PassRate(submittedCount: 3, passedCount: 1).Should().Be(33.3m);
        }
    }
}
