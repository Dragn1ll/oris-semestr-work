using Application.Utils;
using Domain.Models;

namespace Application.Interfaces.Services.HelperServices;

public interface IGoogleTokenStore
{
    Task<Result<GoogleTokenData?>> GetTokenAsync(Guid userId);
    Task<Result> StoreTokenAsync(Guid userId, string accessToken, string? refreshToken, DateTime expiresAt);
    Task<Result> RemoveTokenAsync(Guid userId);
    Task<Result> UpdateTokenAsync(Guid userId, string accessToken, string? refreshToken, DateTime expiresAt);
    Task<Result<bool>> HasTokenAsync(Guid userId);
}