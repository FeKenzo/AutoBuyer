using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AutoBuyer.Application.Monitoring;

public sealed class StoreAccessPolicy : IStoreAccessPolicy
{
    private readonly IStoreMonitoringStateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StoreAccessPolicy> _logger;

    public StoreAccessPolicy(
        IStoreMonitoringStateRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<StoreAccessPolicy> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> CanExecuteAsync(
        Uri productUri,
        CancellationToken cancellationToken)
    {
        var state = await _repository.GetByHostAsync(
            productUri.Host,
            cancellationToken);

        if (state is null)
            return true;

        var canExecute = state.CanExecute(DateTime.UtcNow);

        if (!canExecute)
        {
            _logger.LogWarning(
                "Consulta bloqueada pela política de acesso. Host: {Host}. Status: {Status}. Próxima tentativa: {NextAttempt}.",
                state.Host,
                state.Status,
                state.NextAllowedAttemptAt);
        }

        return canExecute;
    }

    public async Task RegisterSuccessAsync(
        Uri productUri,
        CancellationToken cancellationToken)
    {
        var state = await GetOrCreateAsync(
            productUri.Host,
            cancellationToken);

        state.RegisterSuccess(DateTime.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RegisterFailureAsync(
        Uri productUri,
        ProductPriceResult result,
        CancellationToken cancellationToken)
    {
        var state = await GetOrCreateAsync(
            productUri.Host,
            cancellationToken);

        var now = DateTime.UtcNow;

        if (result.RequiresManualAction &&
            state.ConsecutiveFailures >= 2)
        {
            state.MarkAsRequiresManualAction(
                result.Error ?? "Ação manual necessária.",
                now);
        }
        else
        {
            state.RegisterFailure(
                result.Error ?? "Falha desconhecida.",
                result.HttpStatusCode,
                now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<StoreMonitoringState> GetOrCreateAsync(
        string host,
        CancellationToken cancellationToken)
    {
        var state = await _repository.GetByHostAsync(
            host,
            cancellationToken);

        if (state is not null)
            return state;

        state = new StoreMonitoringState(host);

        await _repository.AddAsync(
            state,
            cancellationToken);

        return state;
    }
}