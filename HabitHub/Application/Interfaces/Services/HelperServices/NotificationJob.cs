using Application.Dto_s.Habit;
using Application.Interfaces.Services.MainServices;
using Microsoft.Extensions.Logging;
using Quartz;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Application.Interfaces.Services.HelperServices;

[DisallowConcurrentExecution]
public class NotificationJob(
    ITelegramChatStore chatStore,
    TelegramBotClient botClient,
    IHabitService habitService,
    ILogger<NotificationJob> logger
    ) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var connectionsResult = await chatStore.GetAllConnectionsAsync();
            if (!connectionsResult.IsSuccess || connectionsResult.Value == null) return;

            var habitsCache = new Dictionary<Guid, IEnumerable<HabitInfoDto>>();
            foreach (var userId in connectionsResult.Value.Keys)
            {
                var habits = await habitService.GetAllByUserIdAsync(userId);
                if (habits.IsSuccess) habitsCache[userId] = habits.Value!;
            }

            foreach (var (userId, chatId) in connectionsResult.Value)
            {
                if (!habitsCache.TryGetValue(userId, out var habits)) continue;
                
                foreach (var habit in habits.Where(h => h.IsActive))
                {
                    await SendHabitNotification(botClient, chatId, habit);
                    await Task.Delay(200);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка в задаче отправки уведомлений");
        }
    }

    private async Task SendHabitNotification(
        TelegramBotClient botClient, 
        long chatId, 
        HabitInfoDto habit)
    {
        var keyboard = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData(
                "✅ Выполнил", 
                $"progress_{habit.Id}")
        );

        await botClient.SendMessage(
            chatId,
            $"🎯 Цель привычки: {habit.Goal}",
            replyMarkup: keyboard);
    }
}