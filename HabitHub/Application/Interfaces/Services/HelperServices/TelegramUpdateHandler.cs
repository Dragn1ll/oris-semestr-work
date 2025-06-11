using Application.Dto_s.Habit;
using Application.Interfaces.Services.MainServices;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Application.Interfaces.Services.HelperServices;

public class TelegramUpdateHandler(
    TelegramBotClient bot,
    IUserService userService,
    IHabitService habitService,
    ITelegramChatStore chatStore,
    ILogger<TelegramUpdateHandler> logger)
{
    public async Task HandleAsync(Update update)
    {
        try
        {
            switch (update)
            {
                case { Message: { } message }:
                    await HandleMessageAsync(message);
                    break;
                
                case { CallbackQuery: { } callback }:
                    await HandleCallbackAsync(callback);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка обработки обновления");
        }
    }

    private async Task HandleMessageAsync(Message message)
    {
        if (message.Text is not { } text) return;
        var chatId = message.Chat.Id;

        if (text.StartsWith("/start"))
        {
            await bot.SendMessage(chatId, "🔑 Введите ваш UserId (GUID):");
            return;
        }

        if (!Guid.TryParse(text, out var userId))
        {
            await bot.SendMessage(chatId, "❌ Неверный формат GUID. Попробуйте еще раз:");
            return;
        }

        var userResult = await userService.GetByIdAsync(userId);
        if (userResult is not { IsSuccess: true, Value: not null })
        {
            await bot.SendMessage(chatId, "❌ Пользователь не найден. Введите правильный UserId:");
            return;
        }

        var storeResult = await chatStore.StoreConnectionAsync(
            userId,
            chatId,
            TimeSpan.FromDays(30));

        if (storeResult.IsSuccess)
        {
            await bot.SendMessage(chatId, "🔔 Уведомления подключены!");
        }
        else
        {
            await bot.SendMessage(chatId, "⚠️ Ошибка подключения. Попробуйте позже.");
        }
    }

    private async Task HandleCallbackAsync(CallbackQuery callback)
    {
        if (!callback.Data!.StartsWith("progress_") ||
            !Guid.TryParse(callback.Data[9..], out var habitId))
            return;

        var userIdResult = await chatStore.GetUserIdByChatAsync(callback.Message!.Chat.Id);
        if (userIdResult is not { IsSuccess: true, Value: { } userId })
        {
            await bot.AnswerCallbackQuery(callback.Id, "Сессия истекла. Перезапустите /start");
            return;
        }

        var progressDto = new HabitProgressAddDto
        {
            HabitId = habitId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            PercentageCompletion = 100f
        };
        
        var result = await habitService.AddProgressAsync(userId, progressDto);

        await bot.AnswerCallbackQuery(
            callback.Id,
            result.IsSuccess ? "✅ Прогресс сохранён!" : "❌ Ошибка сохранения",
            showAlert: true);
    }
}