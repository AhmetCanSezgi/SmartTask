using SmartTask.Domain.Common;
using SmartTask.Domain.Exceptions;

namespace SmartTask.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Username { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public ICollection<TaskItem> Tasks { get; private set; } = new List<TaskItem>();

    private User() { }

    public static User Create(string username, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("Username cannot be empty.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("Invalid email address.");

        return new User
        {
            Username = username.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash
        };
    }
}
