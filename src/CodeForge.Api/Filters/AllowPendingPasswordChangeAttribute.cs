namespace CodeForge.Api.Filters
{
    /// <summary>
    /// Opts an [Authorize]'d endpoint out of PasswordChangeRequiredFilter — reachable
    /// even while the caller's account still has MustChangePassword set. Applied to
    /// change-password itself and to /auth/me (so the forced screen can read identity).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AllowPendingPasswordChangeAttribute : Attribute
    {
    }
}
