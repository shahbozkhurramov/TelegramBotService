using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotService.Services;

public class BotService : BackgroundService
{
    private readonly ILogger<BotService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TelegramBotClient _botClient;

    public BotService(ILogger<BotService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _botClient = new TelegramBotClient(_configuration["TelegramBot:Token"]);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Telegram Bot Service...");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // Receive all update types
        };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: stoppingToken
        );

        _logger.LogInformation("Telegram Bot Service started.");

        await Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message!.Text?.ToLower() == "/start")
        {
            await SendWebAppButtonAsync(update.Message.Chat.Id, cancellationToken);
        }
    }

    private async Task SendWebAppButtonAsync(long chatId, CancellationToken cancellationToken)
    {
        var webAppUrl = _configuration["TelegramBot:WebAppUrl"] ?? string.Empty;

        var webAppButton = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithWebApp("Open Web App", new WebAppInfo { Url = webAppUrl })
            }
        });

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Click the button below to open the Web App:",
            replyMarkup: webAppButton,
            cancellationToken: cancellationToken
        );
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogError(errorMessage);
        return Task.CompletedTask;
    }
}
