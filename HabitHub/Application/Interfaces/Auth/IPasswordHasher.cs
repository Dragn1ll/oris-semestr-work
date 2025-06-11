namespace Application.Interfaces.Auth;

public interface IPasswordHasher
{
    string GetPasswordHash(string password);
    bool Validate(string password, string passwordHash);
}