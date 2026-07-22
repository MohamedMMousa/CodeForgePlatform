using CodeForge.Application.Common.Constants;

namespace CodeForge.Application.Sessions.Common
{
    public static class SessionValidationRules
    {
        public static readonly string[] ValidTypes =
        {
            SessionTypes.Live,
            SessionTypes.InPerson,
            SessionTypes.RecordedLesson
        };
    }
}
