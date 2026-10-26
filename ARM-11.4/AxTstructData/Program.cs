using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ARMCommon.Filter;
using ARMCommon.ActionFilter;
using ARMCommon.Middleware;
using System.Diagnostics;
using ARMCommon.Services;
using AxTstructData;

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddJsonFile("appsettings.json", optional: false);

    var env = builder.Environment.EnvironmentName;

    var _configuration = builder.Configuration;    

    var services = builder.Services;


    services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });

    services.AddDbContext<DataContext>();

    //Singleton services
    services.AddSingleton<IConfiguration>(_configuration);
    services.AddSingleton<IJwtKeyProvider, JwtKeyProvider>();
    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();

    services.AddSingleton<IAppSettingsService, AppSettingsService>();
    services.AddSingleton<IAppSettingsHelper, AppSettingsHelper>();

    //Scoped services
    services.AddScoped<IRedisHelper, RedisHelper>();
    services.AddScoped<IARMLogTraceService, ARMLogTraceService>();
    services.AddScoped<IAxGetRepository, AxGetRepository>();
    services.AddScoped<IAxGetService, AxGetService>();
    services.AddScoped<IAxPutRepository, AxPutRepository>();
    services.AddScoped<IAxPutService, AxPutService>();
    services.AddScoped<IAxDeleteRepository, AxDeleteRepository>();
    services.AddScoped<IAxDeleteService, AxDeleteService>();
    services.AddScoped<IAxUserPermissionService, AxUserPermissionService>();
    services.AddScoped<IAxAttachmentsRepository, AxAttachmentsRepository>();
    services.AddScoped<IAxAttachmentsService, AxAttachmentsService>();
    services.AddScoped<AppSettingsValidaterService>();
    services.AddScoped<IAxParserProvider, AxParserProvider>();

    services.AddScoped<Utils>();
    services.AddScoped<IRedisCacheHelper, RedisCacheHelper>();

    //Transient services
    services.AddTransient<ValidateSessionFilter>();
    services.AddTransient<ApiResponseFilter>();

    services.AddHttpContextAccessor();
    services.AddHttpClient();
    services.AddAuthorization();

    services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(2, 1);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    });

    services.AddVersionedApiExplorer(
        options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

    services.AddAuthentication(auth =>
    {
        auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]))
        };
    });

    var app = builder.Build();
    app.UseStaticFiles();
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseRouting();

    app.UseCors("CorsPolicy");
    app.UseAuthentication();
    app.UseAuthorization();

    app.Use((context, next) =>
    {
        context.Request.EnableBuffering();
        return next();
    });

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
    app.Run();
}
catch (Exception ex)
{
    LogToEventViewer(ex);
    LogToFile(ex);
    throw; // rethrow so app still fails fast
}
void LogToEventViewer(Exception ex)
{
    try
    {
        string source = "AxTstructData";
        string log = "Application";
        string message = $"[{DateTime.Now}] Unhandled Exception: {ex.Message}\nStack Trace: {ex.StackTrace}";
        if (!EventLog.SourceExists(source))
        {
            EventLog.CreateEventSource(source, log);
        }
        EventLog.WriteEntry(source, message, EventLogEntryType.Error);
    }
    catch (Exception ex1)
    {
        // If logging to Event Viewer fails, we silently ignore to avoid recursive failures.
        Console.WriteLine($"[{DateTime.Now}] Unhandled Exception: {ex1.Message}\nStack Trace: {ex1.StackTrace}");
    }
}
static void LogToFile(Exception ex)
{
    try
    {
        var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDir); // creates if not exists

        var logFile = Path.Combine(logDir, $"startup-error_{DateTime.Now:dd-MMM-yyyy}.txt");

        File.AppendAllText(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}"
        );

    }
    catch (Exception ex1)
    {
        // If logging to LogToFile fails, we silently ignore to avoid recursive failures.
        Console.WriteLine($"[{DateTime.Now}] Unhandled Exception: {ex1.Message}\nStack Trace: {ex1.StackTrace}");
    }
}