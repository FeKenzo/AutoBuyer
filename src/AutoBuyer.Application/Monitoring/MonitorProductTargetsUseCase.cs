using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Monitoring;
using AutoBuyer.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AutoBuyer.Application.UseCases.Monitoring;

public sealed class MonitorProductTargetsUseCase
    : IMonitorProductTargetsUseCase
{
    private readonly IProductTargetRepository _productTargetRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly IProductPriceReader _priceReader;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MonitorProductTargetsUseCase> _logger;

    public MonitorProductTargetsUseCase(
        IProductTargetRepository productTargetRepository,
        IPriceHistoryRepository priceHistoryRepository,
        IProductPriceReader priceReader,
        IUnitOfWork unitOfWork,
        ILogger<MonitorProductTargetsUseCase> logger)
    {
        _productTargetRepository = productTargetRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _priceReader = priceReader;
        _unitOfWork = unitOfWork;
        _logger = logger;
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
            if (cancellationToken.IsCancellationRequested)
                return;

            try
            {
                var result = await _priceReader.ReadAsync(
                    target.ProductUrl,
                    cancellationToken);

                if (!result.Success || result.Price is null)
                {
                    _logger.LogWarning(
                        "Não foi possível obter o preço de {ProductName}. Motivo: {Error}",
                        target.Name,
                        result.Error);

                    continue;
                }

                var priceHistory = new PriceHistory(
                    target.Id,
                    result.Price.Value,
                    result.IsAvailable,
                    DateTime.UtcNow);

                await _priceHistoryRepository.AddAsync(
                    priceHistory,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);

                if (result.Price <= target.TargetPrice)
                {
                    _logger.LogWarning(
                        "Preço-alvo atingido. Produto: {ProductName}. Atual: {CurrentPrice}. Alvo: {TargetPrice}",
                        target.Name,
                        result.Price,
                        target.TargetPrice);
                }
                else
                {
                    _logger.LogInformation(
                        "Preço capturado. Produto: {ProductName}. Atual: {CurrentPrice}. Alvo: {TargetPrice}",
                        target.Name,
                        result.Price,
                        target.TargetPrice);
                }
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
}