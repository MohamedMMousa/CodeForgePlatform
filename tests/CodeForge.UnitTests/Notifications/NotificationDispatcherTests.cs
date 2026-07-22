using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Notifications;
using CodeForge.Infrastructure.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CodeForge.UnitTests.Notifications
{
    public class NotificationDispatcherTests
    {
        private class FakeChannel : INotificationChannel
        {
            private readonly bool _throws;
            public string ChannelName { get; }
            public int CallCount { get; private set; }

            public FakeChannel(string name, bool throws = false)
            {
                ChannelName = name;
                _throws = throws;
            }

            public Task SendAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default)
            {
                CallCount++;
                if (_throws)
                {
                    throw new InvalidOperationException("Simulated channel failure.");
                }
                return Task.CompletedTask;
            }
        }

        private static NotificationEvent SampleEvent()
            => new(NotificationEventType.EnrollmentApproved, "student@example.com", "Ada Lovelace", null,
                new Dictionary<string, string> { ["courseTitle"] = "Python Fundamentals" });

        [Fact]
        public async Task DispatchAsync_CallsEveryRegisteredChannel()
        {
            var email = new FakeChannel("email");
            var whatsapp = new FakeChannel("whatsapp");
            var dispatcher = new NotificationDispatcher(new[] { email, whatsapp }, NullLogger<NotificationDispatcher>.Instance);

            await dispatcher.DispatchAsync(SampleEvent());

            email.CallCount.Should().Be(1);
            whatsapp.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task DispatchAsync_OneChannelThrows_OtherChannelsStillRun()
        {
            var failing = new FakeChannel("failing", throws: true);
            var healthy = new FakeChannel("healthy");
            var dispatcher = new NotificationDispatcher(new[] { failing, healthy }, NullLogger<NotificationDispatcher>.Instance);

            await dispatcher.DispatchAsync(SampleEvent());

            healthy.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task DispatchAsync_ChannelThrows_DoesNotPropagateToCaller()
        {
            var failing = new FakeChannel("failing", throws: true);
            var dispatcher = new NotificationDispatcher(new[] { failing }, NullLogger<NotificationDispatcher>.Instance);

            var act = async () => await dispatcher.DispatchAsync(SampleEvent());

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task DispatchAsync_NoChannelsRegistered_CompletesWithoutError()
        {
            var dispatcher = new NotificationDispatcher(Array.Empty<INotificationChannel>(), NullLogger<NotificationDispatcher>.Instance);

            var act = async () => await dispatcher.DispatchAsync(SampleEvent());

            await act.Should().NotThrowAsync();
        }
    }
}
