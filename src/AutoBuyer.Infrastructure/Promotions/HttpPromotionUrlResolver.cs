using AutoBuyer.Application.Promotions.Resolution;
using Microsoft.Extensions.Logging;

namespace AutoBuyer.Infrastructure.Promotions;

public sealed class HttpPromotionUrlResolver
    : IPromotionUrlResolver
{
    private static readonly HashSet<string> ShortenerHosts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "amzn.to",
            "meli.la",
            "s.shopee.com.br",
            "magalu.onelink.me"
        };

    private static readonly HashSet<string> UntrustedShortenerHosts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "bit.ly",
            "t.co",
            "tinyurl.com"
        };

    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpPromotionUrlResolver> _logger;

    public HttpPromotionUrlResolver(
        HttpClient httpClient,
        ILogger<HttpPromotionUrlResolver> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PromotionUrlResolution> ResolveAsync(
        string originalUrl,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(
                originalUrl,
                UriKind.Absolute,
                out var originalUri) ||
            originalUri.Scheme is not ("http" or "https"))
        {
            return PromotionUrlResolution.Failed(
                originalUrl,
                "A URL da promoção é inválida.");
        }

        if (UntrustedShortenerHosts.Contains(originalUri.Host))
        {
            return PromotionUrlResolution.Failed(
                originalUri.AbsoluteUri,
                "O encurtador genérico exige revisão manual.");
        }

        if (!ShortenerHosts.Contains(originalUri.Host))
            return PromotionUrlResolution.Unchanged(originalUri.AbsoluteUri);

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                originalUri);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var resolvedUri = response.RequestMessage?.RequestUri;

            if (resolvedUri is null)
            {
                return PromotionUrlResolution.Failed(
                    originalUri.AbsoluteUri,
                    "O redirecionamento não retornou uma URL final.");
            }

            if (ShortenerHosts.Contains(resolvedUri.Host))
            {
                return PromotionUrlResolution.Failed(
                    originalUri.AbsoluteUri,
                    "O link encurtado não retornou o endereço do produto.");
            }

            return PromotionUrlResolution.Resolved(
                originalUri.AbsoluteUri,
                resolvedUri.AbsoluteUri);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Não foi possível resolver a URL encurtada {Url}.",
                originalUri);

            return PromotionUrlResolution.Failed(
                originalUri.AbsoluteUri,
                "Não foi possível resolver o link encurtado.");
        }
    }
}
