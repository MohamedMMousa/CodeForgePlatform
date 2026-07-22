using CodeForge.Application.Common.Notifications;

namespace CodeForge.Infrastructure.Notifications
{
    /// <summary>
    /// Pure event-to-email rendering, kept separate from EmailNotificationChannel so it's
    /// directly unit-testable without any DI/IEmailSender wiring. Mirrors the inline-HTML
    /// style already used by ForgotPasswordCommandHandler.
    /// </summary>
    public static class EmailNotificationTemplates
    {
        public static (string Subject, string HtmlBody) Render(NotificationEvent notificationEvent)
        {
            return notificationEvent.EventType switch
            {
                NotificationEventType.EnrollmentApproved => RenderEnrollmentApproved(notificationEvent),
                NotificationEventType.EnrollmentRejected => RenderEnrollmentRejected(notificationEvent),
                NotificationEventType.CertificateIssued => RenderCertificateIssued(notificationEvent),
                NotificationEventType.AssignmentGraded => RenderAssignmentGraded(notificationEvent),
                NotificationEventType.InstructorAccountCreated => RenderInstructorAccountCreated(notificationEvent),
                _ => throw new InvalidOperationException(
                    $"No email template registered for event type '{notificationEvent.EventType}'.")
            };
        }

        private static (string, string) RenderEnrollmentApproved(NotificationEvent evt)
        {
            var courseTitle = evt.Data.GetValueOrDefault("courseTitle", "your course");
            var temporaryPassword = evt.Data.GetValueOrDefault("temporaryPassword", "");
            var accessExpiresAt = evt.Data.GetValueOrDefault("accessExpiresAt", "");

            var credentialsParagraph = string.IsNullOrEmpty(temporaryPassword)
                ? ""
                : $"<p>A new account was created for you. Your temporary password is " +
                  $"<strong>{temporaryPassword}</strong> — you'll be asked to change it on first login.</p>";

            var expiryParagraph = string.IsNullOrEmpty(accessExpiresAt)
                ? ""
                : $"<p>Your access to this course runs through {accessExpiresAt}.</p>";

            var body =
                $"<p>Hello {evt.RecipientName},</p>" +
                $"<p>Your enrollment in <strong>{courseTitle}</strong> has been approved!</p>" +
                credentialsParagraph +
                expiryParagraph +
                "<p>See you in class.</p>";

            return ($"You're enrolled in {courseTitle}", body);
        }

        private static (string, string) RenderEnrollmentRejected(NotificationEvent evt)
        {
            var courseTitle = evt.Data.GetValueOrDefault("courseTitle", "your requested course");
            var reason = evt.Data.GetValueOrDefault("rejectionReason", "");

            var body =
                $"<p>Hello {evt.RecipientName},</p>" +
                $"<p>We're unable to approve your enrollment request for <strong>{courseTitle}</strong> at this time.</p>" +
                (string.IsNullOrEmpty(reason) ? "" : $"<p>Reason: {reason}</p>") +
                "<p>If you have questions, please contact us.</p>";

            return ($"Update on your {courseTitle} enrollment request", body);
        }

        private static (string, string) RenderCertificateIssued(NotificationEvent evt)
        {
            var courseTitle = evt.Data.GetValueOrDefault("courseTitle", "your course");
            var tier = evt.Data.GetValueOrDefault("tier", "participation");
            var serialNumber = evt.Data.GetValueOrDefault("serialNumber", "");
            var tierLabel = tier == "completion" ? "Completion" : "Participation";

            var body =
                $"<p>Hello {evt.RecipientName},</p>" +
                $"<p>Congratulations! You've been issued a <strong>{tierLabel}</strong> certificate for " +
                $"<strong>{courseTitle}</strong>.</p>" +
                (string.IsNullOrEmpty(serialNumber) ? "" : $"<p>Certificate serial: {serialNumber}</p>") +
                "<p>You can view and download it from your account under \"My certificates.\"</p>";

            return ($"Your {courseTitle} certificate is ready", body);
        }

        private static (string, string) RenderAssignmentGraded(NotificationEvent evt)
        {
            var assignmentTitle = evt.Data.GetValueOrDefault("assignmentTitle", "your assignment");
            var courseTitle = evt.Data.GetValueOrDefault("courseTitle", "your course");
            var score = evt.Data.GetValueOrDefault("score", "");
            var feedback = evt.Data.GetValueOrDefault("feedback", "");

            var scoreParagraph = string.IsNullOrEmpty(score)
                ? ""
                : $"<p>Score: <strong>{score}</strong></p>";
            var feedbackParagraph = string.IsNullOrEmpty(feedback)
                ? ""
                : $"<p>Instructor feedback: {feedback}</p>";

            var body =
                $"<p>Hello {evt.RecipientName},</p>" +
                $"<p>Your submission for <strong>{assignmentTitle}</strong> ({courseTitle}) has been graded.</p>" +
                scoreParagraph +
                feedbackParagraph;

            return ($"{assignmentTitle} has been graded", body);
        }

        private static (string, string) RenderInstructorAccountCreated(NotificationEvent evt)
        {
            var temporaryPassword = evt.Data.GetValueOrDefault("temporaryPassword", "");

            var body =
                $"<p>Hello {evt.RecipientName},</p>" +
                "<p>An instructor account has been created for you on CodeForge Academy.</p>" +
                $"<p>Your temporary password is <strong>{temporaryPassword}</strong> — you'll be asked to change it on first login.</p>" +
                "<p>Sign in whenever you're ready to get started.</p>";

            return ("Your CodeForge Academy instructor account is ready", body);
        }
    }
}
