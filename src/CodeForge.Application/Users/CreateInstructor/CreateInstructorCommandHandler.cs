using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Notifications;
using CodeForge.Application.Users.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Users.CreateInstructor
{
    public class CreateInstructorCommandHandler : IRequestHandler<CreateInstructorCommand, UserDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITemporaryPasswordGenerator _temporaryPasswordGenerator;
        private readonly INotificationDispatcher _notificationDispatcher;

        public CreateInstructorCommandHandler(
            ICodeForgeDbContext context,
            ICurrentUserService currentUserService,
            IPasswordHasher passwordHasher,
            ITemporaryPasswordGenerator temporaryPasswordGenerator,
            INotificationDispatcher notificationDispatcher)
        {
            _context = context;
            _currentUserService = currentUserService;
            _passwordHasher = passwordHasher;
            _temporaryPasswordGenerator = temporaryPasswordGenerator;
            _notificationDispatcher = notificationDispatcher;
        }

        public async Task<UserDto> Handle(CreateInstructorCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var normalizedEmail = request.Email.Trim().ToLower();

            var emailInUse = await _context.Users
                .AnyAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
            if (emailInUse)
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }

            var temporaryPassword = _temporaryPasswordGenerator.Generate();

            var instructor = new User
            {
                Email = normalizedEmail,
                FullName = request.FullName.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                PasswordHash = _passwordHasher.HashPassword(temporaryPassword),
                Role = Roles.Instructor,
                IsActive = true,
                MustChangePassword = true
            };

            _context.Users.Add(instructor);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "user.instructor_created", nameof(User), instructor.Id,
                new { instructor.Email, instructor.FullName }));

            await _context.SaveChangesAsync(cancellationToken);

            await _notificationDispatcher.DispatchAsync(
                new NotificationEvent(
                    NotificationEventType.InstructorAccountCreated,
                    instructor.Email,
                    instructor.FullName,
                    instructor.Phone,
                    new Dictionary<string, string> { ["temporaryPassword"] = temporaryPassword }),
                cancellationToken);

            return UserMapping.ToDto(instructor);
        }

        private Guid GetCurrentUserId()
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated admin could not be resolved.");
            }

            return userId;
        }
    }
}
