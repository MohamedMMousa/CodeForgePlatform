using CodeForge.Application.Assignments.Common;
using FluentAssertions;
using Xunit;

namespace CodeForge.UnitTests.Assignments
{
    public class AssignmentGradingCalculatorTests
    {
        [Fact]
        public void CalculateAutoScore_AllTestCasesPassed_Returns100()
        {
            var outcomes = new List<AssignmentGradingCalculator.TestCaseOutcome>
            {
                new(Points: 5, Passed: true),
                new(Points: 5, Passed: true),
            };

            AssignmentGradingCalculator.CalculateAutoScore(outcomes).Should().Be(100);
        }

        [Fact]
        public void CalculateAutoScore_WeightsByPoints_NotByTestCaseCount()
        {
            var outcomes = new List<AssignmentGradingCalculator.TestCaseOutcome>
            {
                new(Points: 8, Passed: true),
                new(Points: 2, Passed: false),
            };

            AssignmentGradingCalculator.CalculateAutoScore(outcomes).Should().Be(80);
        }

        [Fact]
        public void CalculateAutoScore_NoTestCases_ReturnsZero_NotDivideByZero()
        {
            AssignmentGradingCalculator.CalculateAutoScore(new List<AssignmentGradingCalculator.TestCaseOutcome>()).Should().Be(0);
        }

        [Fact]
        public void ComputePassed_NotGraded_ReturnsNull()
        {
            AssignmentGradingCalculator.ComputePassed(finalScore: null, passScore: 70).Should().BeNull();
        }

        [Fact]
        public void ComputePassed_NoPassThreshold_ReturnsNull()
        {
            AssignmentGradingCalculator.ComputePassed(finalScore: 95, passScore: null).Should().BeNull();
        }

        [Fact]
        public void ComputePassed_ScoreAtThreshold_ReturnsTrue()
        {
            AssignmentGradingCalculator.ComputePassed(finalScore: 70, passScore: 70).Should().BeTrue();
        }

        [Fact]
        public void ComputePassed_ScoreBelowThreshold_ReturnsFalse()
        {
            AssignmentGradingCalculator.ComputePassed(finalScore: 69, passScore: 70).Should().BeFalse();
        }
    }
}
