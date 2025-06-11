using Application.Enums;
using Application.Interfaces.Services.HelperServices;
using Application.Utils;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Application.Services.HelperServices;

public class RedisTelegramChatStore(
    IConnectionMultiplexer redis,
    ILogger<RedisTelegramChatStore> logger
    ) : ITelegramChatStore
{
    private const string KeyPrefix = "telegram_chat:";
    private const string ReverseKeyPrefix = "telegram_user:";
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly IServer _redisServer = redis.GetServer(redis.GetEndPoints().First());

    public async Task<Result> StoreConnectionAsync(Guid userId, long chatId, TimeSpan expiry)
    {
        try
        {
            await _database.StringSetAsync(
                $"{KeyPrefix}{userId}",
                chatId,
                expiry);

            await _database.StringSetAsync(
                $"{ReverseKeyPrefix}{chatId}",
                userId.ToString(),
                expiry);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка сохранения подключения в Redis");
            return Result.Failure(new Error(ErrorType.ServerError, 
                "Ошибка сохранения подключения"));
        }
    }

    public async Task<Result<long?>> GetChatIdAsync(Guid userId)
    {
        try
        {
            var value = await _database.StringGetAsync($"{KeyPrefix}{userId}");
            return value.HasValue && long.TryParse(value, out var chatId)
                ? Result<long?>.Success(chatId)
                : Result<long?>.Success(null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка получения chatId из Redis");
            return Result<long?>.Failure(new Error(ErrorType.ServerError, 
                "Ошибка получения подключения"));
        }
    }

    public async Task<Result<Guid?>> GetUserIdByChatAsync(long chatId)
    {
        try
        {
            var userIdStr = await _database.StringGetAsync($"{ReverseKeyPrefix}{chatId}");
            return Guid.TryParse(userIdStr, out var userId)
                ? Result<Guid?>.Success(userId)
                : Result<Guid?>.Success(null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка получения userId из Redis");
            return Result<Guid?>.Failure(new Error(ErrorType.ServerError, 
                "Ошибка получения пользователя"));
        }
    }

    public async Task<Result<Dictionary<Guid, long>>> GetAllConnectionsAsync()
    {
        try
        {
            var connections = new Dictionary<Guid, long>();
            var keys = _redisServer.Keys(pattern: $"{KeyPrefix}*");

            foreach (var key in keys)
            {
                var value = await _database.StringGetAsync(key);
                if (!value.HasValue) continue;

                var userId = Guid.Parse(key.ToString().Replace(KeyPrefix, ""));
                connections[userId] = long.Parse(value);
            }

            return Result<Dictionary<Guid, long>>.Success(connections);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка получения всех подключений из Redis");
            return Result<Dictionary<Guid, long>>.Failure(
                new Error(ErrorType.ServerError, "Ошибка получения подключений"));
        }
    }

    public async Task<Result> RemoveConnectionAsync(Guid userId)
    {
        try
        {
            var chatIdResult = await GetChatIdAsync(userId);
            if (chatIdResult is { IsSuccess: true, Value: { } chatId })
            {
                await _database.KeyDeleteAsync($"{ReverseKeyPrefix}{chatId}");
            }

            await _database.KeyDeleteAsync($"{KeyPrefix}{userId}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка удаления подключения из Redis");
            return Result.Failure(new Error(ErrorType.ServerError, 
                "Ошибка удаления подключения"));
        }
    }
}