using Application.Interfaces.Auth;

namespace Infrastructure.Auth;

public class PasswordHasher : IPasswordHasher
{
    public string GetPasswordHash(string password) => BCrypt.Net.BCrypt.EnhancedHashPassword(password);
    
    public bool Validate(string password, string passwordHash) => 
        BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
}