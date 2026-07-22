using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Materials.Common
{
    /// <summary>
    /// A material attaches to exactly one of a Module or a Session (chk_material_target
    /// — see docs/DATABASE.md §6). Resolves whichever parent was given down to the
    /// owning Course, with Instructors/Enrollments loaded for authorization checks.
    /// </summary>
    public static class MaterialParentResolver
    {
        public static async Task<Course> ResolveCourseAsync(
            ICodeForgeDbContext context,
            Guid? moduleId,
            Guid? sessionId,
            CancellationToken cancellationToken)
        {
            if (moduleId.HasValue)
            {
                var module = await context.Modules
                    .Include(m => m.Course).ThenInclude(c => c.Instructors)
                    .Include(m => m.Course).ThenInclude(c => c.Enrollments)
                    .FirstOrDefaultAsync(m => m.Id == moduleId.Value, cancellationToken);

                if (module is null)
                {
                    throw new KeyNotFoundException("Module was not found.");
                }

                return module.Course;
            }

            var session = await context.Sessions
                .Include(s => s.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(s => s.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Enrollments)
                .FirstOrDefaultAsync(s => s.Id == sessionId!.Value, cancellationToken);

            if (session is null)
            {
                throw new KeyNotFoundException("Session was not found.");
            }

            return session.Module.Course;
        }
    }
}
