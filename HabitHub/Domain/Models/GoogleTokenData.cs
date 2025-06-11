namespace Domain.Models;

public class GoogleTokenData
{
    public string AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}