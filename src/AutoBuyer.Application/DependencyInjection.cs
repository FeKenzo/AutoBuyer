using AutoBuyer.Application.Monitoring;
using AutoBuyer.Application.UseCases.Monitoring;
using AutoBuyer.Application.UseCases.ProductTargets.ChangeMonitoringStatus;
using AutoBuyer.Application.UseCases.ProductTargets.Create;
using AutoBuyer.Application.UseCases.ProductTargets.Delete;
using AutoBuyer.Application.UseCases.ProductTargets.GetAll;
using AutoBuyer.Application.UseCases.ProductTargets.GetById;
using AutoBuyer.Application.UseCases.ProductTargets.Update;
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
            IUpdateProductTargetUseCase,
            UpdateProductTargetUseCase>();

        services.AddScoped<
            IChangeMonitoringStatusUseCase,
            ChangeMonitoringStatusUseCase>();

        services.AddScoped<
            IDeleteProductTargetUseCase,
            DeleteProductTargetUseCase>();

        services.AddScoped<
            IMonitorProductTargetsUseCase,
            MonitorProductTargetsUseCase>();
       
        services.AddScoped<
            IStoreAccessPolicy,
            StoreAccessPolicy>();

        return services;
    }
}