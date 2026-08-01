namespace CodeForge.Application.Common.Exceptions
{
    /// <summary>
    /// Thrown when an unsafe request carrying an auth cookie doesn't echo the CSRF
    /// cookie value back in the expected header. Maps to 403 with a distinct error
    /// code — see ExceptionHandlingMiddleware.
    /// </summary>
    public class CsrfValidationException : Exception
    {
        public CsrfValidationException()
            : base("CSRF token missing or invalid.")
        {
        }
    }
}
