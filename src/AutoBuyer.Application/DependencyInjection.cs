using AutoBuyer.Application.Monitoring;
using AutoBuyer.Application.Promotions.Parsing;
using AutoBuyer.Application.UseCases.Monitoring;
using AutoBuyer.Application.UseCases.ProductTargets.ChangeMonitoringStatus;
using AutoBuyer.Application.UseCases.ProductTargets.Create;
using AutoBuyer.Application.UseCases.ProductTargets.Delete;
using AutoBuyer.Application.UseCases.ProductTargets.GetAll;
using AutoBuyer.Application.UseCases.ProductTargets.GetById;
using AutoBuyer.Application.UseCases.ProductTargets.Update;
using AutoBuyer.Application.UseCases.Promotions.CreateProductTarget;
using AutoBuyer.Application.UseCases.Promotions.GetAll;
using AutoBuyer.Application.UseCases.Promotions.Ignore;
using AutoBuyer.Application.UseCases.Promotions.ImportMessage;
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

        services.AddScoped<
            IPromotionMessageParser,
            TelegramPromotionParser>();

        services.AddScoped<
            IPromotionMessageParser,
            TelegramPromotionParser>();

        services.AddScoped<
            IImportPromotionMessageUseCase,
            ImportPromotionMessageUseCase>();

        services.AddScoped<
            IGetAllPromotionCandidatesUseCase,
            GetAllPromotionCandidatesUseCase>();

        services.AddScoped<
            ICreateProductTargetFromPromotionUseCase,
            CreateProductTargetFromPromotionUseCase>();

        services.AddScoped<
            IIgnorePromotionCandidateUseCase,
            IgnorePromotionCandidateUseCase>();

        return services;
    }
}