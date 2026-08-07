using AutoBuyer.Application;
using AutoBuyer.Infrastructure;
using AutoBuyer.TelegramIngestion.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<TelegramIngestionOptions>(
    builder.Configuration.GetSection(
        TelegramIngestionOptions.SectionName));

builder.Services.AddHostedService<TelegramIngestionWorker>();

var host = builder.Build();

host.Run();
