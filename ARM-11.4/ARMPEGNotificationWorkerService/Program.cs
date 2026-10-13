using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ARMPEGNotificationWorkerService";
});

builder.Services.AddSystemd();
builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();
builder.Services.AddSingleton<IAppSettingsHelper, AppSettingsHelper>();
builder.Services.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();
builder.Services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
builder.Services.AddSingleton<Utils>();
builder.Services.AddScoped<DataContext>();
builder.Services.AddSingleton<API>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();