using Application.Utils;

namespace Application.Interfaces.Services.HelperServices;

public interface ITelegramChatStore
{
    Task<Result> StoreConnectionAsync(Guid userId, long chatId, TimeSpan expiry);
    Task<Result<long?>> GetChatIdAsync(Guid userId);
    Task<Result<Guid?>> GetUserIdByChatAsync(long chatId);
    Task<Result<Dictionary<Guid, long>>> GetAllConnectionsAsync();
    Task<Result> RemoveConnectionAsync(Guid userId);
}