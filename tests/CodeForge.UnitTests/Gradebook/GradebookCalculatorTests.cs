using CodeForge.Application.Gradebook.Common;
using CodeForge.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CodeForge.UnitTests.Gradebook
{
    public class GradebookCalculatorTests
    {
        private static readonly Guid StudentId = Guid.NewGuid();

        [Fact]
        public void BuildAssignmentGrades_GradedAboveThreshold_PassedIsTrue()
        {
            var assignment = new Assignment { Id = Guid.NewGuid(), Title = "Above", PassScore = 70 };
            var submissions = new List<AssignmentSubmission>
            {
                new()
                {
                    AssignmentId = assignment.Id,
                    StudentId = StudentId,
                    AttemptNumber = 1,
                    FinalScore = 85,
                    GradedAt = DateTime.UtcNow
                }
            };

            var grades = GradebookCalculator.BuildAssignmentGrades(StudentId, new[] { assignment }, submissions);

            grades.Should().ContainSingle().Which.Passed.Should().BeTrue();
        }

        [Fact]
        public void BuildAssignmentGrades_GradedBelowThreshold_PassedIsFalse()
        {
            var assignment = new Assignment { Id = Guid.NewGuid(), Title = "Below", PassScore = 70 };
            var submissions = new List<AssignmentSubmission>
            {
                new()
                {
                    AssignmentId = assignment.Id,
                    StudentId = StudentId,
                    AttemptNumber = 1,
                    FinalScore = 50,
                    GradedAt = DateTime.UtcNow
                }
            };

            var grades = GradebookCalculator.BuildAssignmentGrades(StudentId, new[] { assignment }, submissions);

            grades.Should().ContainSingle().Which.Passed.Should().BeFalse();
        }

        [Fact]
        public void BuildAssignmentGrades_NoSubmission_PassedIsNull()
        {
            var assignment = new Assignment { Id = Guid.NewGuid(), Title = "Ungraded", PassScore = 70 };

            var grades = GradebookCalculator.BuildAssignmentGrades(StudentId, new[] { assignment }, new List<AssignmentSubmission>());

            grades.Should().ContainSingle().Which.Passed.Should().BeNull();
        }

        [Fact]
        public void BuildAssignmentGrades_GradedButNoPassThreshold_PassedIsNull()
        {
            var assignment = new Assignment { Id = Guid.NewGuid(), Title = "Practice", PassScore = null };
            var submissions = new List<AssignmentSubmission>
            {
                new()
                {
                    AssignmentId = assignment.Id,
                    StudentId = StudentId,
                    AttemptNumber = 1,
                    FinalScore = 95,
                    GradedAt = DateTime.UtcNow
                }
            };

            var grades = GradebookCalculator.BuildAssignmentGrades(StudentId, new[] { assignment }, submissions);

            grades.Should().ContainSingle().Which.Passed.Should().BeNull();
        }
    }
}
