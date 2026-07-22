namespace CodeForge.Application.Authentication.Common
{
    public record CurrentUserResponse(
        Guid UserId,
        string Email,
        string FullName,
        string? Phone,
        string Role,
        bool MustChangePassword);
}
