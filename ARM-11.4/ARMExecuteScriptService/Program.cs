//using ARMCommon.Helpers.RabbitMq;
//using ARMCommon.Helpers;
//using ARMCommon.Interface;
//using ARMCommon.Services;

//var builder = Host.CreateApplicationBuilder(args);

//builder.Services.AddWindowsService(options =>
//{
//    options.ServiceName = "ARMExecuteScriptService";
//});

//builder.Services.AddSystemd();
//builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();
//builder.Services.AddSingleton<IAppSettingsHelper, AppSettingsHelper>();
//builder.Services.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();
//builder.Services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
//builder.Services.AddSingleton<Utils>();
////builder.Services.AddSingleton<IARMLogTraceService, ARMLogTraceService>();
//builder.Services.AddScoped<DataContext>();
//builder.Services.AddSingleton<API>();

//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();
//host.Run();

using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Services;

var builder = Host.CreateApplicationBuilder(args);
string configFile = args.Length > 0 ? args[0] : "appsettings.json";
builder.Configuration.AddJsonFile(configFile, optional: false, reloadOnChange: true);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ARMExecuteScriptService";
});
builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();
builder.Services.AddSingleton<IAppSettingsHelper, AppSettingsHelper>();
builder.Services.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();
builder.Services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
builder.Services.AddSingleton<Utils>();
builder.Services.AddScoped<DataContext>();
builder.Services.AddSingleton<API>();
builder.Services.AddHostedService<Worker>();
//builder.Build().Run();
try
{
    var host = builder.Build();
    //File.AppendAllText(@"D:\Publish\Logs\ServiceDebug.txt", "BUILD OK\n");
    host.Run();
}
catch (Exception ex)
{
    //File.AppendAllText(@"D:\Publish\Logs\ProgramCrash.txt", ex.ToString());
}
