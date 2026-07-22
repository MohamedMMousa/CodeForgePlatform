namespace CodeForge.Application.Authentication.Common
{
    public record AuthResponse(
        Guid UserId,
        string Email,
        string FullName,
        string Role,
        string AccessToken,
        string RefreshToken,
        DateTime RefreshTokenExpiresAt,
        bool MustChangePassword);
}
