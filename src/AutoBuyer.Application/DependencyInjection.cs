using AutoBuyer.Application.UseCases.Monitoring;
using AutoBuyer.Application.UseCases.ProductTargets.Create;
using AutoBuyer.Application.UseCases.ProductTargets.GetAll;
using AutoBuyer.Application.UseCases.ProductTargets.GetById;
using Microsoft.Extensions.DependencyInjection;

namespace AutoBuyer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<
            ICreateProductTargetUseCase,
            CreateProductTargetUseCase>();

        services.AddScoped<
            IGetAllProductTargetsUseCase,
            GetAllProductTargetsUseCase>();

        services.AddScoped<
            IGetProductTargetByIdUseCase,
            GetProductTargetByIdUseCase>();

        services.AddScoped<
            IMonitorProductTargetsUseCase,
            MonitorProductTargetsUseCase>();

        return services;
    }
}