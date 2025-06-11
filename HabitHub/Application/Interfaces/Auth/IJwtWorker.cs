using System.Security.Claims;
using Application.Dto_s.User;

namespace Application.Interfaces.Auth;

public interface IJwtWorker
{
    string GenerateJwtToken(UserAuthInfoDto user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}