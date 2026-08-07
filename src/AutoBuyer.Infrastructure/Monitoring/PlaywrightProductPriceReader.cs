using AutoBuyer.Application.Monitoring;
using AutoBuyer.Infrastructure.Monitoring.Extractors;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutoBuyer.Infrastructure.Monitoring;

public sealed class PlaywrightProductPriceReader
    : IProductPriceReader
{
    private readonly StorePriceExtractorResolver _resolver;
    private readonly ILogger<PlaywrightProductPriceReader> _logger;

    public PlaywrightProductPriceReader(
        StorePriceExtractorResolver resolver,
        ILogger<PlaywrightProductPriceReader> logger)
    {
        _resolver = resolver;
        _logger = logger;
    }

    public async Task<ProductPriceResult> ReadAsync(
        string productUrl,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(
                productUrl,
                UriKind.Absolute,
                out var productUri))
        {
            return ProductPriceResult.Failed(
                "A URL do produto é inválida.");
        }

        try
        {
            using var playwright = await Playwright.CreateAsync();

            await using var browser =
                await playwright.Chromium.LaunchAsync(
                    new BrowserTypeLaunchOptions
                    {
                        Headless = false,
                        SlowMo = 100
                    });

            await using var context =
                await browser.NewContextAsync(
                    new BrowserNewContextOptions
                    {
                        Locale = "pt-BR",
                        TimezoneId = "America/Sao_Paulo",
                        ViewportSize = new ViewportSize
                        {
                            Width = 1366,
                            Height = 900
                        }
                    });

            var page = await context.NewPageAsync();

            var response = await page.GotoAsync(
                productUri.ToString(),
                new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 45_000
                });

            if (response is null)
            {
                return ProductPriceResult.Failed(
                    "O navegador não recebeu resposta da página.");
            }

            if (!response.Ok)
            {
                return ProductPriceResult.Failed(
                    $"A página retornou HTTP {response.Status}.",
                    response.Status);
            }

            await WaitForPageAsync(page);

            var extractor = _resolver.Resolve(productUri);

            _logger.LogInformation(
                "Usando o extrator {ExtractorName} para {Host}.",
                extractor.GetType().Name,
                productUri.Host);

            var extractionResult = await extractor.ExtractAsync(
                page,
                productUri,
                cancellationToken);

            if (extractionResult.Success)
            {
                return extractionResult;
            }

            var challenge = await DetectChallengeAsync(page);

            if (challenge is not null)
            {
                return ProductPriceResult.Failed(
                    challenge,
                    response.Status,
                    requiresManualAction: true);
            }

            return extractionResult;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (PlaywrightException exception)
        {
            _logger.LogWarning(
                exception,
                "O Playwright falhou ao consultar {ProductUrl}.",
                productUrl);

            return ProductPriceResult.Failed(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Erro ao consultar {ProductUrl}.",
                productUrl);

            return ProductPriceResult.Failed(exception.Message);
        }
    }

    private static async Task WaitForPageAsync(IPage page)
    {
        try
        {
            await page.WaitForLoadStateAsync(
                LoadState.NetworkIdle,
                new PageWaitForLoadStateOptions
                {
                    Timeout = 8_000
                });
        }
        catch (TimeoutException)
        {
            // Algumas lojas mantêm conexões abertas.
            // O DOM já está disponível, então continuamos.
        }

        await page.WaitForTimeoutAsync(1_500);
    }

    private static async Task<string?> DetectChallengeAsync(
    IPage page)
    {
        var challengeSelectors = new Dictionary<string, string>
        {
            ["iframe[src*='captcha']"] = "iframe de CAPTCHA",
            ["iframe[src*='recaptcha']"] = "Google reCAPTCHA",
            ["iframe[src*='challenges.cloudflare.com']"] = "Cloudflare Turnstile",
            [".g-recaptcha"] = "Google reCAPTCHA",
            ["[data-sitekey]"] = "componente com site key",
            ["#cf-challenge-running"] = "challenge do Cloudflare",
            ["#challenge-running"] = "challenge de segurança",
            ["input[name='cf-turnstile-response']"] = "Cloudflare Turnstile"
        };

        foreach (var challengeSelector in challengeSelectors)
        {
            var locator = page.Locator(challengeSelector.Key);

            var count = Math.Min(
                await locator.CountAsync(),
                5);

            for (var index = 0; index < count; index++)
            {
                try
                {
                    if (await locator.Nth(index).IsVisibleAsync())
                    {
                        return
                            $"A página apresentou um challenge visível: " +
                            $"{challengeSelector.Value}.";
                    }
                }
                catch (PlaywrightException)
                {
                    // O elemento pode ter desaparecido durante a validação.
                }
            }
        }

        var title = await page.TitleAsync();

        var challengeTitles = new[]
        {
        "Just a moment",
        "Attention Required",
        "Access Denied",
        "Verifique se você é humano",
        "Checking your browser"
    };

        var matchingTitle = challengeTitles.FirstOrDefault(
            value => title.Contains(
                value,
                StringComparison.OrdinalIgnoreCase));

        if (matchingTitle is not null)
        {
            return
                $"A página apresentou um título de challenge: " +
                $"'{matchingTitle}'.";
        }

        return null;
    }
}