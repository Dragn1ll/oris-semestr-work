using System.Text.Json;
using Application.Enums;
using Application.Interfaces.Services.HelperServices;
using Application.Utils;
using Domain.Models;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Application.Services.HelperServices;

public class RedisGoogleTokenStore(IConnectionMultiplexer redis, ILogger<RedisGoogleTokenStore> logger) 
    : IGoogleTokenStore
{
    private const string KeyPrefix = "user_tokens:";

    public async Task<Result<GoogleTokenData?>> GetTokenAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return Result<GoogleTokenData?>.Failure(new Error(ErrorType.BadRequest, 
                "UserId не может быть пустым"));

        try
        {
            var db = redis.GetDatabase();
            var redisKey = GetRedisKey(userId);
            var json = await db.StringGetAsync(redisKey);

            if (json.IsNullOrEmpty)
            {
                logger.LogWarning($"Токен пользователя {userId} не найден");
                return Result<GoogleTokenData?>.Success(null);
            }

            var tokenData = JsonSerializer.Deserialize<GoogleTokenData>(json!);
            logger.LogInformation($"Получен токен пользователя {userId}");
            return Result<GoogleTokenData?>.Success(tokenData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при получении токена пользователя {userId}");
            return Result<GoogleTokenData?>.Failure(new Error(ErrorType.ServerError, 
                "Ошибка при получении токена"));
        }
    }

    public async Task<Result> StoreTokenAsync(Guid userId, string accessToken, string? refreshToken, 
        DateTime expiresAt)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(accessToken))
            return Result.Failure(new Error(ErrorType.BadRequest, 
                "UserId или токен не могут быть пустыми"));

        try
        {
            var db = redis.GetDatabase();
            var redisKey = GetRedisKey(userId);
            var tokenData = new GoogleTokenData
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt
            };

            var json = JsonSerializer.Serialize(tokenData);
            var expiration = expiresAt - DateTime.UtcNow;
            if (expiration <= TimeSpan.Zero)
                expiration = TimeSpan.FromMinutes(1);

            await db.StringSetAsync(redisKey, json, expiration);

            logger.LogInformation($"Сохранён Google токен для пользователя {userId}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при сохранении токена пользователя {userId}");
            return Result.Failure(new Error(ErrorType.ServerError, "Ошибка при сохранении токена"));
        }
    }

    public async Task<Result> RemoveTokenAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure(new Error(ErrorType.BadRequest, "UserId не может быть пустым"));

        try
        {
            var db = redis.GetDatabase();
            var redisKey = GetRedisKey(userId);
            var deleted = await db.KeyDeleteAsync(redisKey);

            if (deleted)
            {
                logger.LogInformation($"Удалён токен пользователя {userId}");
                return Result.Success();
            }

            logger.LogWarning($"Токен для пользователя {userId} не найден при удалении");
            return Result.Failure(new Error(ErrorType.NotFound, 
                $"Токен пользователя {userId} не найден"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при удалении токена пользователя {userId}");
            return Result.Failure(new Error(ErrorType.ServerError, 
                $"Ошибка при удалении токена пользователя {userId}"));
        }
    }
    
    public async Task<Result> UpdateTokenAsync(Guid userId, string accessToken, string? refreshToken, DateTime expiresAt)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogWarning("Попытка обновления токена с невалидными параметрами");
            return Result.Failure(new Error(ErrorType.BadRequest, "Невалидные параметры токена"));
        }

        try
        {
            var db = redis.GetDatabase();
            var redisKey = GetRedisKey(userId);
            
            var existingTokenJson = await db.StringGetAsync(redisKey);
            var existingToken = existingTokenJson.HasValue 
                ? JsonSerializer.Deserialize<GoogleTokenData>(existingTokenJson!) 
                : null;
            
            var updatedToken = new GoogleTokenData
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken ?? existingToken?.RefreshToken,
                ExpiresAt = expiresAt
            };

            var newExpiration = expiresAt - DateTime.UtcNow;
            if (newExpiration <= TimeSpan.Zero)
            {
                logger.LogWarning($"Некорректный срок действия токена для пользователя {userId}");
                newExpiration = TimeSpan.FromMinutes(1);
            }

            await db.StringSetAsync(
                redisKey, 
                JsonSerializer.Serialize(updatedToken), 
                newExpiration);

            logger.LogInformation($"Обновлён Google токен для пользователя {userId}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при обновлении токена пользователя {userId}");
            return Result.Failure(new Error(ErrorType.ServerError, "Ошибка обновления токена"));
        }
    }

    public async Task<Result<bool>> HasTokenAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return Result<bool>.Failure(new Error(ErrorType.BadRequest, 
                "UserId не может быть пустым"));

        try
        {
            var db = redis.GetDatabase();
            var redisKey = GetRedisKey(userId);
            var exists = await db.KeyExistsAsync(redisKey);
            logger.LogInformation($"Проверено наличие токена для пользователя {userId}: {exists}");
            return Result<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при проверке токена пользователя {userId}");
            return Result<bool>.Failure(new Error(ErrorType.ServerError, 
                $"Ошибка при проверке токена пользователя {userId}"));
        }
    }

    private string GetRedisKey(Guid userId) => $"{KeyPrefix}{userId.ToString().ToLower()}";
}