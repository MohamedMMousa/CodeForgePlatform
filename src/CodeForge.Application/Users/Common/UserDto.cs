namespace CodeForge.Application.Users.Common
{
    public record UserDto(
        Guid Id,
        string Email,
        string FullName,
        string? Phone,
        string Role,
        bool IsActive,
        bool MustChangePassword,
        DateTime CreatedAt);
}
