using CodeForge.Application.Assessments.Common;
using FluentAssertions;
using Xunit;

namespace CodeForge.UnitTests.Assessments
{
    public class QuizGradingCalculatorTests
    {
        [Fact]
        public void Calculate_AllCorrect_Returns100Score()
        {
            var result = QuizGradingCalculator.Calculate(totalQuestions: 4, correctCount: 4, passScore: 70);

            result.Score.Should().Be(100);
        }

        [Fact]
        public void Calculate_NoneCorrect_ReturnsZeroScore()
        {
            var result = QuizGradingCalculator.Calculate(totalQuestions: 4, correctCount: 0, passScore: 70);

            result.Score.Should().Be(0);
        }

        [Fact]
        public void Calculate_PartialCorrect_RoundsToNearestPercent()
        {
            var result = QuizGradingCalculator.Calculate(totalQuestions: 3, correctCount: 1, passScore: 70);

            result.Score.Should().Be(33);
        }

        [Fact]
        public void Calculate_ZeroQuestions_ReturnsZeroScore_NotDivideByZero()
        {
            var result = QuizGradingCalculator.Calculate(totalQuestions: 0, correctCount: 0, passScore: 70);

            result.Score.Should().Be(0);
        }

        [Fact]
        public void Calculate_ScoreAtOrAbovePassScore_MarksPassed()
        {
            var result = QuizGradingCalculator.Calculate(totalQuestions: 10, correctCount: 7, passScore: 70);

            result.Passed.Should().BeTrue();
        }

        [Fact]
        public void Calculate_ScoreBelowPassScore_MarksNotPassed()
        {
            var result = QuizGradingCalculator.Calculate(totalQuestions: 10, correctCount: 6, passScore: 70);

            result.Passed.Should().BeFalse();
        }

        [Fact]
        public void Calculate_NoPassScoreConfigured_LeavesPassedNull()
        {
            var result = QuizGradingCalculator.Calculate(totalQuestions: 10, correctCount: 6, passScore: null);

            result.Passed.Should().BeNull();
        }
    }
}
