using CodeForge.Domain.Entities;

namespace CodeForge.Application.Sessions.Common
{
    public static class SessionMapping
    {
        public static SessionDto ToDto(Session session)
        {
            return new SessionDto(
                session.Id,
                session.ModuleId,
                session.Type,
                session.Title,
                session.Description,
                session.OrderIndex,
                session.ScheduledAt,
                session.DurationMinutes,
                session.JoinLink,
                session.Location,
                session.VideoUrl,
                session.InstructorId,
                session.Instructor?.FullName,
                session.Materials.Count,
                session.CreatedAt,
                session.UpdatedAt);
        }
    }
}
