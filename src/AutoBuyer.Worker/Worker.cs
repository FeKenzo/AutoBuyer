using AutoBuyer.Application.UseCases.Monitoring;

namespace AutoBuyer.Worker;

public sealed class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;
    private readonly TimeSpan _interval;

    public Worker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var intervalSeconds =
            configuration.GetValue<int?>(
                "Monitoring:IntervalSeconds") ?? 60;

        _interval = TimeSpan.FromSeconds(
            Math.Max(intervalSeconds, 10));
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "AutoBuyer Worker iniciado. Intervalo: {Interval}.",
            _interval);

        using var timer = new PeriodicTimer(_interval);

        await RunMonitoringAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunMonitoringAsync(stoppingToken);
        }
    }

    private async Task RunMonitoringAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var useCase =
                scope.ServiceProvider.GetRequiredService<
                    IMonitorProductTargetsUseCase>();

            await useCase.ExecuteAsync(cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Encerramento normal da aplicação.
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Erro durante o ciclo de monitoramento.");
        }
    }
}