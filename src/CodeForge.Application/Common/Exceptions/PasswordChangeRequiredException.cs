namespace CodeForge.Application.Common.Exceptions
{
    /// <summary>
    /// Thrown when an authenticated request hits an endpoint that requires
    /// <see cref="Domain.Entities.User.MustChangePassword"/> to be false. Maps to
    /// 403 with a distinct error code — see ExceptionHandlingMiddleware.
    /// </summary>
    public class PasswordChangeRequiredException : Exception
    {
        public PasswordChangeRequiredException()
            : base("This account must change its password before continuing.")
        {
        }
    }
}
