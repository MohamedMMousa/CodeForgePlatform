using CodeForge.Application.Certificates.Common;
using CodeForge.Application.Common.Constants;
using FluentAssertions;
using Xunit;

namespace CodeForge.UnitTests.Certificates
{
    public class CertificateEligibilityCalculatorTests
    {
        [Fact]
        public void Evaluate_AttendanceMet_AndAllAssessmentsPassed_IsCompletion()
        {
            var result = CertificateEligibilityCalculator.Evaluate(
                attendanceRate: 80m,
                courseThreshold: 75m,
                requiredAssessmentPassStates: new[] { true, true });

            result.Tier.Should().Be(CertificateTiers.Completion);
            result.AttendanceMet.Should().BeTrue();
            result.AssessmentsPassed.Should().BeTrue();
        }

        [Fact]
        public void Evaluate_AttendanceBelowThreshold_IsParticipation()
        {
            var result = CertificateEligibilityCalculator.Evaluate(
                attendanceRate: 70m,
                courseThreshold: 75m,
                requiredAssessmentPassStates: new[] { true, true });

            result.Tier.Should().Be(CertificateTiers.Participation);
            result.AttendanceMet.Should().BeFalse();
        }

        [Fact]
        public void Evaluate_OneAssessmentFailed_IsParticipation()
        {
            var result = CertificateEligibilityCalculator.Evaluate(
                attendanceRate: 90m,
                courseThreshold: 75m,
                requiredAssessmentPassStates: new[] { true, false });

            result.Tier.Should().Be(CertificateTiers.Participation);
            result.AssessmentsPassed.Should().BeFalse();
        }

        [Fact]
        public void Evaluate_ExactlyAtThreshold_CountsAsMet()
        {
            var result = CertificateEligibilityCalculator.Evaluate(
                attendanceRate: 75m,
                courseThreshold: 75m,
                requiredAssessmentPassStates: Array.Empty<bool>());

            result.AttendanceMet.Should().BeTrue();
            result.Tier.Should().Be(CertificateTiers.Completion);
        }

        [Fact]
        public void Evaluate_NoRequiredAssessments_AssessmentBarVacuouslyMet()
        {
            var result = CertificateEligibilityCalculator.Evaluate(
                attendanceRate: 80m,
                courseThreshold: null,
                requiredAssessmentPassStates: Array.Empty<bool>());

            result.AssessmentsPassed.Should().BeTrue();
            result.Tier.Should().Be(CertificateTiers.Completion);
        }

        [Fact]
        public void Evaluate_NullCourseThreshold_UsesPlatformDefault()
        {
            // Platform default is 75; 74 should fall short of it.
            var result = CertificateEligibilityCalculator.Evaluate(
                attendanceRate: 74m,
                courseThreshold: null,
                requiredAssessmentPassStates: new[] { true });

            result.AttendanceThreshold.Should().Be(CertificateDefaults.AttendanceThreshold);
            result.AttendanceMet.Should().BeFalse();
            result.Tier.Should().Be(CertificateTiers.Participation);
        }

        [Fact]
        public void Evaluate_CourseOverridesThreshold_UsesCourseValue()
        {
            var result = CertificateEligibilityCalculator.Evaluate(
                attendanceRate: 60m,
                courseThreshold: 50m,
                requiredAssessmentPassStates: new[] { true });

            result.AttendanceThreshold.Should().Be(50m);
            result.AttendanceMet.Should().BeTrue();
            result.Tier.Should().Be(CertificateTiers.Completion);
        }
    }
}
