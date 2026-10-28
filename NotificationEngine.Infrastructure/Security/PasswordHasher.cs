using NotificationEngine.Core.Security;
using BC = BCrypt.Net.BCrypt;

namespace NotificationEngine.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
        => BC.HashPassword(password);

    public bool Verify(string password, string? passwordHash)
    {
        if (string.IsNullOrEmpty(passwordHash))
            return false;

        return BC.Verify(password, passwordHash);
    }
}
