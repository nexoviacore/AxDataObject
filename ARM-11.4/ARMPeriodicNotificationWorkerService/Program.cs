using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Services;
using ARMPeriodicNotificationWorkerService;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ARMPeriodicNotificationWorkerService";
});

builder.Services.AddSystemd();
builder.Services.AddSingleton<RedisJobStore>();
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