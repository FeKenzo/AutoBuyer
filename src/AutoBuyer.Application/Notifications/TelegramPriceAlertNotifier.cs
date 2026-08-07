using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AutoBuyer.Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoBuyer.Infrastructure.Notifications;

public sealed class TelegramPriceAlertNotifier
    : IPriceAlertNotifier
{
    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramPriceAlertNotifier> _logger;

    public TelegramPriceAlertNotifier(
        HttpClient httpClient,
        IOptions<TelegramOptions> options,
        ILogger<TelegramPriceAlertNotifier> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotifyAsync(
        PriceAlertNotification notification,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Telegram desabilitado. Notificação de {ProductName} ignorada.",
                notification.ProductName);

            return;
        }

        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            throw new InvalidOperationException(
                "O token do bot do Telegram não foi configurado.");
        }

        if (string.IsNullOrWhiteSpace(_options.ChatId))
        {
            throw new InvalidOperationException(
                "O ChatId do Telegram não foi configurado.");
        }

        var endpoint = new Uri(
            _httpClient.BaseAddress!,
            $"./bot{_options.BotToken}/sendMessage");

        var request = new TelegramSendMessageRequest(
            _options.ChatId,
            BuildMessage(notification),
            ParseMode: "HTML",
            DisableWebPagePreview: false);

        using var response = await _httpClient.PostAsJsonAsync(
            endpoint,
            request,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "Notificação enviada pelo Telegram para {ProductName}.",
                notification.ProductName);

            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);

        throw new HttpRequestException(
            $"Falha ao enviar notificação pelo Telegram. " +
            $"HTTP {(int)response.StatusCode}: {responseBody}");
    }

    private static string BuildMessage(
        PriceAlertNotification notification)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");

        var currentPrice = notification.CurrentPrice.ToString(
            "C",
            culture);

        var targetPrice = notification.TargetPrice.ToString(
            "C",
            culture);

        var discountFromTarget =
            notification.TargetPrice - notification.CurrentPrice;

        var capturedAt = notification.CapturedAt
            .ToLocalTime()
            .ToString("dd/MM/yyyy 'às' HH:mm", culture);

        return $"""
                🚨 <b>Preço-alvo atingido!</b>

                🛒 <b>{EscapeHtml(notification.ProductName)}</b>
                🏪 Loja: {EscapeHtml(notification.StoreName)}

                💰 Preço atual: <b>{currentPrice}</b>
                🎯 Preço-alvo: {targetPrice}
                📉 Abaixo do alvo: {discountFromTarget.ToString("C", culture)}

                🕐 Capturado em: {capturedAt}

                🔗 <a href="{EscapeHtml(notification.ProductUrl)}">Abrir produto</a>
                """;
    }

    private static string EscapeHtml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
    }

    private sealed record TelegramSendMessageRequest(
        [property: JsonPropertyName("chat_id")]
        string ChatId,

        [property: JsonPropertyName("text")]
        string Text,

        [property: JsonPropertyName("parse_mode")]
        string ParseMode,

        [property: JsonPropertyName("disable_web_page_preview")]
        bool DisableWebPagePreview);
}