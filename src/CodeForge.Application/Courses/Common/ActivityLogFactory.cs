using System.Text.Json;
using CodeForge.Domain.Entities;

namespace CodeForge.Application.Courses.Common
{
    public static class ActivityLogFactory
    {
        public static ActivityLog Create(
            Guid userId,
            string action,
            Guid courseId,
            object? metadata = null)
        {
            return new ActivityLog
            {
                UserId = userId,
                Action = action,
                EntityType = nameof(Course),
                EntityId = courseId,
                Metadata = metadata is null
                    ? null
                    : JsonDocument.Parse(JsonSerializer.Serialize(metadata))
            };
        }
    }
}
