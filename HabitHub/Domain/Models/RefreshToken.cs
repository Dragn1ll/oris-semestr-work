namespace Domain.Models;

public class RefreshToken(
    Guid id,
    Guid userId,
    string token,
    DateTime expires,
    bool isRevoked
    )
{
    public Guid Id { get; } = id;
    public Guid UserId { get; } = userId;
    public string Token { get; } = token;
    public DateTime Expires { get; } = expires;
    public bool IsRevoked { get; } = isRevoked;

    public static RefreshToken Create(Guid id, Guid userId, string token, DateTime expires, bool isRevoked)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty");
        
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty");
        
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty");
        
        return new RefreshToken(id, userId, token, expires, isRevoked);
    }
}