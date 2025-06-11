using Application.Interfaces.Services.HelperServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace Application.Services.MainServices;

public class TelegramBotHostedService(
    TelegramBotClient bot,
    IServiceProvider services,
    ILogger<TelegramBotHostedService> logger
    ) : IHostedService
{
    private CancellationTokenSource? _cts;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: new ReceiverOptions(),
            cancellationToken: _cts.Token);
        
        logger.LogInformation("Telegram бот запущен");
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient botClient, 
        Update update, 
        CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<TelegramUpdateHandler>();
        await handler.HandleAsync(update);
    }

    private Task HandleErrorAsync(
        ITelegramBotClient botClient, 
        Exception ex, 
        CancellationToken ct)
    {
        logger.LogError(ex, "Ошибка Telegram бота");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        logger.LogInformation("Telegram бот остановлен");
        return Task.CompletedTask;
    }
}