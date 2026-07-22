using CodeForge.Application.Common.Models;
using CodeForge.Application.Common.Notifications;
using CodeForge.Infrastructure.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CodeForge.UnitTests.Notifications
{
    public class WhatsAppNotificationChannelTests
    {
        private static NotificationEvent SampleEvent(string? phone = "+201000000000")
            => new(NotificationEventType.EnrollmentApproved, "student@example.com", "Ada Lovelace", phone,
                new Dictionary<string, string> { ["courseTitle"] = "Python Fundamentals" });

        [Fact]
        public async Task SendAsync_WhenDisabled_NoOpsInsteadOfThrowing()
        {
            var settings = Options.Create(new WhatsAppSettings { Enabled = false });
            var channel = new WhatsAppNotificationChannel(settings, NullLogger<WhatsAppNotificationChannel>.Instance);

            var act = async () => await channel.SendAsync(SampleEvent());

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SendAsync_WhenEnabledButNoRecipientPhone_NoOps()
        {
            var settings = Options.Create(new WhatsAppSettings { Enabled = true, PhoneNumberId = "123", AccessToken = "token" });
            var channel = new WhatsAppNotificationChannel(settings, NullLogger<WhatsAppNotificationChannel>.Instance);

            var act = async () => await channel.SendAsync(SampleEvent(phone: null));

            await act.Should().NotThrowAsync();
        }
    }
}
