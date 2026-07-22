using System.Text.Json;
using CodeForge.Domain.Entities;

namespace CodeForge.Application.Common
{
    /// <summary>
    /// Generic activity-log builder for modules beyond Courses (which has its own
    /// entity-typed factory at Courses/Common/ActivityLogFactory.cs — left as-is).
    /// </summary>
    public static class ActivityLogFactory
    {
        public static ActivityLog Create(
            Guid userId,
            string action,
            string entityType,
            Guid entityId,
            object? metadata = null)
        {
            return new ActivityLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Metadata = metadata is null
                    ? null
                    : JsonDocument.Parse(JsonSerializer.Serialize(metadata))
            };
        }
    }
}
