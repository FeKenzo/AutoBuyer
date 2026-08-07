using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Monitoring;
using AutoBuyer.Application.Notifications;
using AutoBuyer.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AutoBuyer.Application.UseCases.Monitoring;

public sealed class MonitorProductTargetsUseCase
    : IMonitorProductTargetsUseCase
{
    private readonly IProductTargetRepository _productTargetRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly IProductPriceReader _priceReader;
    private readonly IPriceAlertNotifier _priceAlertNotifier;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MonitorProductTargetsUseCase> _logger;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public MonitorProductTargetsUseCase(
        IProductTargetRepository productTargetRepository,
        IPriceHistoryRepository priceHistoryRepository,
        IProductPriceReader priceReader,
        IPriceAlertNotifier priceAlertNotifier,
        IUnitOfWork unitOfWork,
        ILogger<MonitorProductTargetsUseCase> logger,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _productTargetRepository = productTargetRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _priceReader = priceReader;
        _priceAlertNotifier = priceAlertNotifier;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var targets =
            await _productTargetRepository.GetMonitoringEnabledAsync(
                cancellationToken);

        _logger.LogInformation(
            "Iniciando monitoramento de {TargetCount} produtos.",
            targets.Count);

        foreach (var target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await MonitorTargetAsync(
                    target,
                    cancellationToken);
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
                    "Erro ao monitorar o produto {ProductName}.",
                    target.Name);
            }
        }
    }

    private async Task MonitorTargetAsync(
        ProductTarget target,
        CancellationToken cancellationToken)
    {
        // Precisamos consultar antes de inserir o preço novo.
        var previousPriceHistory =
            await _priceHistoryRepository
                .GetLatestByProductTargetIdAsync(
                    target.Id,
                    cancellationToken);

        var result = await _priceReader.ReadAsync(
            target.ProductUrl,
            cancellationToken);

        if (!Uri.TryCreate(
            target.ProductUrl,
            UriKind.Absolute,
            out var productUri))
        {
            _logger.LogWarning(
                "URL inválida para o produto {ProductName}.",
                target.Name);

            return;
        }

        var canExecute =
            await _storeAccessPolicy.CanExecuteAsync(
                productUri,
                cancellationToken);

        if (!canExecute)
            return;

        if (!result.Success || result.Price is null)
        {
            await _storeAccessPolicy.RegisterFailureAsync(
                productUri,
                result,
                cancellationToken);

            _logger.LogWarning(
                "Não foi possível obter o preço de {ProductName}. Motivo: {Error}",
                target.Name,
                result.Error);

            return;
        }

        await _storeAccessPolicy.RegisterSuccessAsync(
            productUri,
            cancellationToken);

        var currentPrice = result.Price.Value;
        var capturedAt = DateTime.UtcNow;

        var priceHistory = new PriceHistory(
            target.Id,
            currentPrice,
            result.IsAvailable,
            capturedAt);

        await _priceHistoryRepository.AddAsync(
            priceHistory,
            cancellationToken);

        // Primeiro persistimos a captura.
        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        var targetReached =
            currentPrice <= target.TargetPrice;

        var targetWasReachedPreviously =
            previousPriceHistory is not null &&
            previousPriceHistory.Price <= target.TargetPrice;

        var crossedTarget =
            targetReached &&
            !targetWasReachedPreviously;

        if (!targetReached)
        {
            _logger.LogInformation(
                "Preço capturado. Produto: {ProductName}. Atual: {CurrentPrice}. Alvo: {TargetPrice}",
                target.Name,
                currentPrice,
                target.TargetPrice);

            return;
        }

        _logger.LogWarning(
            "Preço-alvo atingido. Produto: {ProductName}. Atual: {CurrentPrice}. Alvo: {TargetPrice}",
            target.Name,
            currentPrice,
            target.TargetPrice);

        if (!crossedTarget)
        {
            _logger.LogInformation(
                "Notificação não enviada para {ProductName}, pois o preço já estava abaixo do alvo.",
                target.Name);

            return;
        }

        var notification = new PriceAlertNotification(
            target.Id,
            target.Name,
            target.Store?.Name ?? "Loja não identificada",
            target.ProductUrl,
            currentPrice,
            target.TargetPrice,
            capturedAt);

        try
        {
            await _priceAlertNotifier.NotifyAsync(
                notification,
                cancellationToken);
        }
        catch (Exception exception)
        {
            // A captura permanece salva mesmo se o Telegram falhar.
            _logger.LogError(
                exception,
                "O preço de {ProductName} foi salvo, mas a notificação falhou.",
                target.Name);
        }
    }
}