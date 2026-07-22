using CodeForge.Domain.Entities;

namespace CodeForge.Application.Users.Common
{
    public static class UserMapping
    {
        public static UserDto ToDto(User user)
        {
            return new UserDto(
                user.Id,
                user.Email,
                user.FullName,
                user.Phone,
                user.Role,
                user.IsActive,
                user.MustChangePassword,
                user.CreatedAt);
        }
    }
}
