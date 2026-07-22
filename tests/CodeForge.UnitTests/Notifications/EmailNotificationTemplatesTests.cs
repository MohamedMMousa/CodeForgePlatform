using CodeForge.Application.Common.Notifications;
using CodeForge.Infrastructure.Notifications;
using FluentAssertions;
using Xunit;

namespace CodeForge.UnitTests.Notifications
{
    public class EmailNotificationTemplatesTests
    {
        private static NotificationEvent Event(string type, Dictionary<string, string>? data = null)
            => new(type, "student@example.com", "Ada Lovelace", "+201000000000", data ?? new Dictionary<string, string>());

        [Fact]
        public void Render_EnrollmentApproved_IncludesCourseTitleAndGreeting()
        {
            var (subject, body) = EmailNotificationTemplates.Render(
                Event(NotificationEventType.EnrollmentApproved, new() { ["courseTitle"] = "Python Fundamentals" }));

            subject.Should().Contain("Python Fundamentals");
            body.Should().Contain("Ada Lovelace");
            body.Should().Contain("Python Fundamentals");
        }

        [Fact]
        public void Render_EnrollmentApproved_WithTemporaryPassword_IncludesIt()
        {
            var (_, body) = EmailNotificationTemplates.Render(
                Event(NotificationEventType.EnrollmentApproved, new()
                {
                    ["courseTitle"] = "Python Fundamentals",
                    ["temporaryPassword"] = "Sw0rdfish!"
                }));

            body.Should().Contain("Sw0rdfish!");
        }

        [Fact]
        public void Render_EnrollmentApproved_WithoutTemporaryPassword_OmitsCredentialsParagraph()
        {
            var (_, body) = EmailNotificationTemplates.Render(
                Event(NotificationEventType.EnrollmentApproved, new() { ["courseTitle"] = "Python Fundamentals" }));

            body.Should().NotContain("temporary password");
        }

        [Fact]
        public void Render_EnrollmentRejected_IncludesReason()
        {
            var (_, body) = EmailNotificationTemplates.Render(
                Event(NotificationEventType.EnrollmentRejected, new()
                {
                    ["courseTitle"] = "Data Structures",
                    ["rejectionReason"] = "Payment proof unreadable"
                }));

            body.Should().Contain("Payment proof unreadable");
        }

        [Fact]
        public void Render_CertificateIssued_CompletionTier_UsesCompletionLabel()
        {
            var (subject, body) = EmailNotificationTemplates.Render(
                Event(NotificationEventType.CertificateIssued, new()
                {
                    ["courseTitle"] = "Python Fundamentals",
                    ["tier"] = "completion",
                    ["serialNumber"] = "CF-2026-ABC123"
                }));

            subject.Should().Contain("Python Fundamentals");
            body.Should().Contain("Completion");
            body.Should().Contain("CF-2026-ABC123");
        }

        [Fact]
        public void Render_CertificateIssued_ParticipationTier_UsesParticipationLabel()
        {
            var (_, body) = EmailNotificationTemplates.Render(
                Event(NotificationEventType.CertificateIssued, new()
                {
                    ["courseTitle"] = "Python Fundamentals",
                    ["tier"] = "participation"
                }));

            body.Should().Contain("Participation");
            body.Should().NotContain("Completion");
        }

        [Fact]
        public void Render_AssignmentGraded_IncludesScoreAndFeedback()
        {
            var (subject, body) = EmailNotificationTemplates.Render(
                Event(NotificationEventType.AssignmentGraded, new()
                {
                    ["assignmentTitle"] = "FizzBuzz",
                    ["courseTitle"] = "Python Fundamentals",
                    ["score"] = "85",
                    ["feedback"] = "Nice work, watch edge cases."
                }));

            subject.Should().Contain("FizzBuzz");
            body.Should().Contain("85");
            body.Should().Contain("Nice work, watch edge cases.");
        }

        [Fact]
        public void Render_UnknownEventType_Throws()
        {
            var act = () => EmailNotificationTemplates.Render(Event("something.unregistered"));

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
