using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Monitoring;
using AutoBuyer.Application.Notifications;
using AutoBuyer.Application.Promotions.Resolution;
using AutoBuyer.Infrastructure.Data;
using AutoBuyer.Infrastructure.Monitoring;
using AutoBuyer.Infrastructure.Monitoring.Extractors;
using AutoBuyer.Infrastructure.Notifications;
using AutoBuyer.Infrastructure.Promotions;
using AutoBuyer.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoBuyer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "A connection string 'Postgres' não foi configurada.");

        services.AddDbContext<AutoBuyerDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<
            IProductTargetRepository,
            ProductTargetRepository>();

        services.AddScoped<
            IPriceHistoryRepository,
            PriceHistoryRepository>();

        services.AddScoped<
            IStoreRepository,
            StoreRepository>();

        services.AddScoped<IUnitOfWork>(
            provider =>
                provider.GetRequiredService<AutoBuyerDbContext>());

        services.AddScoped<
            IProductPriceReader,
            PlaywrightProductPriceReader>();

        services.AddScoped<
            IStorePriceExtractor,
            PichauProductPriceExtractor>();

        services.AddScoped<
            IStorePriceExtractor,
            TerabyteProductPriceExtractor>();

        services.AddScoped<
            IStorePriceExtractor,
            GenericProductPriceExtractor>();

        services.AddScoped<StorePriceExtractorResolver>();

        services.AddScoped<
            IStoreMonitoringStateRepository,
            StoreMonitoringStateRepository>();

        services.AddScoped<
            IPromotionCandidateRepository,
            PromotionCandidateRepository>();

        services.AddHttpClient<
                IPromotionUrlResolver,
                HttpPromotionUrlResolver>(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "AutoBuyer/1.0 (+price-monitoring)");
                })
            .ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 10
                });

        services.Configure<TelegramOptions>(
            configuration.GetSection(TelegramOptions.SectionName));

        services.AddHttpClient<
                IPriceAlertNotifier,
                TelegramPriceAlertNotifier>(client =>
                {
                    client.BaseAddress = new Uri(
                    "https://api.telegram.org/");

                    client.Timeout = TimeSpan.FromSeconds(15);
                });

        return services;
    }
}
