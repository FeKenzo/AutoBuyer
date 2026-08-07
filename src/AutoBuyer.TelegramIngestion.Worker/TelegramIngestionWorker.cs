using AutoBuyer.Application.Contracts.Requests.Promotions;
using AutoBuyer.Application.UseCases.Promotions.ImportMessage;
using AutoBuyer.Domain.Enums;
using Microsoft.Extensions.Options;
using TL;

namespace AutoBuyer.TelegramIngestion.Worker;

public sealed class TelegramIngestionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramIngestionOptions _options;
    private readonly ILogger<TelegramIngestionWorker> _logger;
    private readonly SemaphoreSlim _messageLock = new(1, 1);

    private WTelegram.Client? _client;
    private WTelegram.UpdateManager? _updateManager;
    private long _channelId;

    public TelegramIngestionWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<TelegramIngestionOptions> options,
        ILogger<TelegramIngestionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "A ingestão do Telegram está desabilitada.");
            return;
        }

        ValidateOptions();
        EnsureStateDirectories();

        WTelegram.Helpers.Log = (severity, message) =>
        {
            if (severity <= 2)
                _logger.LogDebug("WTelegram: {Message}", message);
        };

        _client = new WTelegram.Client(GetTelegramConfiguration);

        try
        {
            _updateManager = _client.WithUpdateManager(
                HandleUpdateAsync,
                Path.GetFullPath(_options.UpdatesStatePath));

            var account = await _client.LoginUserIfNeeded();

            _logger.LogInformation(
                "Conta dedicada conectada ao Telegram. UserId: {UserId}.",
                account.id);

            var dialogs = await _client.Messages_GetAllDialogs();
            dialogs.CollectUsersChats(
                _updateManager.Users,
                _updateManager.Chats);

            var channel = dialogs.chats.Values
                .OfType<Channel>()
                .FirstOrDefault(candidate =>
                    string.Equals(
                        candidate.username,
                        NormalizeUsername(_options.ChannelUsername),
                        StringComparison.OrdinalIgnoreCase));

            if (channel is null)
            {
                throw new InvalidOperationException(
                    $"O canal @{NormalizeUsername(_options.ChannelUsername)} " +
                    "não foi encontrado entre os diálogos da conta dedicada. " +
                    "Inscreva a conta no canal antes de iniciar o worker.");
            }

            _channelId = channel.id;

            _logger.LogInformation(
                "Canal @{ChannelUsername} localizado. ChannelId: {ChannelId}.",
                NormalizeUsername(_options.ChannelUsername),
                _channelId);

            await ImportRecentHistoryAsync(
                channel,
                stoppingToken);

            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Encerramento normal do worker.
        }
        finally
        {
            if (_updateManager is not null)
            {
                _updateManager.SaveState(
                    Path.GetFullPath(_options.UpdatesStatePath));
            }

            if (_client is not null)
                await _client.DisposeAsync();
        }
    }

    private async Task ImportRecentHistoryAsync(
        Channel channel,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(
            _options.InitialHistoryLimit,
            0,
            100);

        if (limit == 0 || _client is null)
            return;

        var history = await _client.Messages_GetHistory(
            channel,
            limit: limit);

        var messages = history.Messages
            .OfType<Message>()
            .OrderBy(message => message.id)
            .ToArray();

        _logger.LogInformation(
            "Importando {MessageCount} publicações recentes do canal.",
            messages.Length);

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ImportMessageAsync(
                message,
                isEdited: false,
                cancellationToken);
        }
    }

    private async Task HandleUpdateAsync(Update update)
    {
        switch (update)
        {
            case UpdateNewMessage newMessage:
                await HandleMessageBaseAsync(
                    newMessage.message,
                    isEdited: false);
                break;

            case UpdateEditMessage editedMessage:
                await HandleMessageBaseAsync(
                    editedMessage.message,
                    isEdited: true);
                break;
        }
    }

    private Task HandleMessageBaseAsync(
        MessageBase messageBase,
        bool isEdited)
    {
        if (messageBase is not Message message ||
            message.peer_id is not PeerChannel channelPeer ||
            channelPeer.channel_id != _channelId)
        {
            return Task.CompletedTask;
        }

        return ImportMessageAsync(
            message,
            isEdited,
            CancellationToken.None);
    }

    private async Task ImportMessageAsync(
        Message message,
        bool isEdited,
        CancellationToken cancellationToken)
    {
        var parserMessage = BuildParserMessage(message);

        if (string.IsNullOrWhiteSpace(parserMessage))
            return;

        await _messageLock.WaitAsync(cancellationToken);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider
                .GetRequiredService<IImportPromotionMessageUseCase>();

            var result = await useCase.ExecuteAsync(
                new ImportPromotionMessageRequest(
                    _channelId,
                    message.id,
                    parserMessage,
                    isEdited),
                cancellationToken);

            if (result.IsDuplicate)
            {
                _logger.LogDebug(
                    "Publicação {MessageId} já processada.",
                    message.id);
                return;
            }

            if (!result.Success || result.Promotion is null)
            {
                _logger.LogWarning(
                    "Publicação {MessageId} rejeitada. Motivo: {Error}",
                    message.id,
                    result.Error);
                return;
            }

            var logLevel = result.Promotion.Status is
                PromotionCandidateStatus.NeedsReview or
                PromotionCandidateStatus.UnsupportedStore
                ? LogLevel.Warning
                : LogLevel.Information;

            _logger.Log(
                logLevel,
                "Publicação {MessageId} processada. Status: {Status}. " +
                "ProductTargetId: {ProductTargetId}.",
                message.id,
                result.Promotion.Status,
                result.Promotion.ProductTargetId);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Erro ao processar a publicação {MessageId}.",
                message.id);
        }
        finally
        {
            _messageLock.Release();
        }
    }

    private string? GetTelegramConfiguration(string what)
    {
        return what switch
        {
            "api_id" => _options.ApiId.ToString(),
            "api_hash" => _options.ApiHash,
            "phone_number" => _options.PhoneNumber,
            "session_pathname" =>
                Path.GetFullPath(_options.SessionPath),
            "verification_code" =>
                Environment.GetEnvironmentVariable(
                    "TELEGRAM_VERIFICATION_CODE"),
            "password" =>
                Environment.GetEnvironmentVariable(
                    "TELEGRAM_2FA_PASSWORD"),
            _ => null
        };
    }

    private static string BuildParserMessage(Message message)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(message.message))
            parts.Add(message.message.Trim());

        if (message.entities is not null)
        {
            parts.AddRange(message.entities
                .OfType<MessageEntityTextUrl>()
                .Select(entity => entity.url)
                .Where(url => !string.IsNullOrWhiteSpace(url)));
        }

        if (message.reply_markup is ReplyInlineMarkup inlineMarkup)
        {
            parts.AddRange(inlineMarkup.rows
                .SelectMany(row => row.buttons)
                .OfType<KeyboardButtonUrl>()
                .Select(button => button.url)
                .Where(url => !string.IsNullOrWhiteSpace(url)));
        }

        return string.Join(
            Environment.NewLine,
            parts.Distinct(StringComparer.Ordinal));
    }

    private void ValidateOptions()
    {
        if (_options.ApiId <= 0 ||
            string.IsNullOrWhiteSpace(_options.ApiHash) ||
            string.IsNullOrWhiteSpace(_options.PhoneNumber))
        {
            throw new InvalidOperationException(
                "Configure TelegramIngestion:ApiId, ApiHash e " +
                "PhoneNumber antes de habilitar o worker.");
        }

        if (string.IsNullOrWhiteSpace(_options.ChannelUsername))
        {
            throw new InvalidOperationException(
                "Configure TelegramIngestion:ChannelUsername.");
        }
    }

    private void EnsureStateDirectories()
    {
        EnsureParentDirectory(_options.SessionPath);
        EnsureParentDirectory(_options.UpdatesStatePath);
    }

    private static void EnsureParentDirectory(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().TrimStart('@');
    }

    public override void Dispose()
    {
        _messageLock.Dispose();
        base.Dispose();
    }
}
