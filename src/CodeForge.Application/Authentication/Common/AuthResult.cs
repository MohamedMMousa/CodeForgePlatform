namespace CodeForge.Application.Authentication.Common
{
    /// <summary>
    /// Internal carrier for the tokens login/refresh/change-password mint — used by
    /// AuthController to write the httpOnly cookies, never serialized to the client.
    /// The response body sent to the client is CurrentUserResponse; see
    /// AuthController.ToCurrentUserResponse.
    /// </summary>
    public record AuthResult(
        Guid UserId,
        string Email,
        string FullName,
        string? Phone,
        string Role,
        string AccessToken,
        string RefreshToken,
        DateTime RefreshTokenExpiresAt,
        bool MustChangePassword);
}
