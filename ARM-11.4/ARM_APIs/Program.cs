
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using ARMCommon.Services;
using ARM_APIs.Interface;
using ARM_APIs.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ARMCommon.Filter;
using ARMCommon.ActionFilter;
using ARMCommon.Middleware;
using ARM_APIs.Services;
using ARM_APIs.Service;
using ARM_APIs.Repositories;
using AxParserApp;
using ARM_APIs.Interfaces;
using AxValidate;
using AxStructDef;
using System.Diagnostics;
//using System.Globalization;
//using Microsoft.AspNetCore.Localization;
try
{
    var builder = WebApplication.CreateBuilder(args);

    //// Set global culture
    //var cultureInfo = new CultureInfo("en-GB");
    //CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
    //CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

    //builder.Services.Configure<RequestLocalizationOptions>(options =>
    //{
    //    options.DefaultRequestCulture = new RequestCulture(cultureInfo);
    //    options.SupportedCultures = new List<CultureInfo> { cultureInfo };
    //    options.SupportedUICultures = new List<CultureInfo> { cultureInfo };
    //});

    // Add the appsettings.json file
    builder.Configuration.AddJsonFile("appsettings.json", optional: false);

    // Load the appropriate settings file based on the environment
    var env = builder.Environment.EnvironmentName;
    //builder.Configuration.AddJsonFile($"appsettings.{env}.json", optional: true);

    var _configuration = builder.Configuration;

    //var emailConfig = _configuration
    //        .GetSection("EmailConfiguration")
    //        .Get<EmailConfiguration>();
    //List<string> signalRIPList = _configuration.GetSection("SingalRIPList").Get<List<string>>();

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

        //options.AddPolicy("CorsPolicy", builder =>
        //{
        //    builder.SetIsOriginAllowed(_ => true)
        //           .AllowAnyMethod()
        //           .AllowAnyHeader()
        //           .AllowCredentials();
        //});
    });

    services.AddDbContext<DataContext>();

    //Singleton services
    services.AddSingleton<IConfiguration>(_configuration);
    services.AddSingleton<IJwtKeyProvider, JwtKeyProvider>();
    services.AddSingleton<IPostgresHelper, PostgresHelper>();
    services.AddSingleton<IOracleHelper, OracleHelper>();
    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
    //services.AddSingleton(emailConfig);

    //services.AddSingleton<IRedisHelper, RedisHelper>();
    services.AddSingleton<IFiles, FileService>();
    services.AddSingleton<IFirebase, FirebaseTokenService>();
    services.AddSingleton<IAppSettingsService, AppSettingsService>();
    services.AddSingleton<IAppSettingsHelper, AppSettingsHelper>();

    //Scoped services
    services.AddScoped<IRedisHelper, RedisHelper>();
    services.AddScoped<IARMLogTraceService, ARMLogTraceService>();

    services.AddScoped<IAxScriptService, AxScriptService>();
    services.AddScoped<AppSettingsValidaterService>();

    //Common scoped services
    services.AddScoped<Utils>();
    services.AddScoped<IRedisCacheHelper, RedisCacheHelper>();

    services.AddScoped<StructDef>();
    services.AddScoped<AxParser>();
    services.AddScoped<Validate>();
    //services.AddScoped<GeneralFunctions>();
    //services.AddScoped<AxStructDefUtils>();

    services.AddScoped<ISqlMetadataRepository, SqlMetadataRepository>();
    services.AddScoped<IPostgresMetadataMapper, PostgresMetadataMapper>();
    services.AddScoped<IOracleMetadataMapper, OracleMetadataMapper>();
    //services.AddScoped<IAxListRepository, AxListRepository>();
    //services.AddScoped<IAxListService, AxListService>();
    //services.AddScoped<IADSDataService, ADSDataService>();
    //services.AddScoped<IADSDataRepository, ADSDataRepository>();

    //Transient services
    //services.AddTransient<IARMAxpertConnection, ARMAxpertConnection>();
    //services.AddTransient<ICardsService, CardsService>();
    services.AddTransient<IARMLogin, ARMLogin>();
    services.AddTransient<IARMPEG, ARMPEG>();
    services.AddTransient<IARMTstruct, ARMTstruct>();
    services.AddTransient<IARMPages, ARMPages>();
    services.AddTransient<IARMMenu, ARMMenu>();
    //services.AddTransient<IARMInlineForm, ARMInlineForm>();
    services.AddTransient<IARMGeoFencing, ARMGeoFencing>();
    //services.AddTransient<IARMGetUserDetail, ARMGetUserDetail>();
    services.AddTransient<INotificationHelper, NotificationHelper>();
    services.AddTransient<IGetData, ARMGetData>();
    services.AddTransient<IAPI, API>();
    services.AddTransient<IARMPushToQueue, PushTOQueue>();
    //services.AddTransient<IARMSigninDetails, ARMSigninDetails>();
    //services.AddTransient<IARMUserGroups, ARMUserGroups>();
    services.AddTransient<IARMUtils, ARMUtils>();
    //services.AddTransient<IARMNotificationService, ARMNotificationService>();
    services.AddTransient<IARMenuV2, ARMMenuV2>();
    //services.AddTransient<IAxpertAIService, AxpertAIService>();
    services.AddTransient<ICardsService, CardsService>();

    //services.AddTransient<IEmailSender, EmailSender>();
    services.AddTransient<ITokenService, TokenService>();
    services.AddTransient<ValidateSessionFilter>();
    services.AddTransient<ApiLoggingFilter>();
    services.AddTransient<ApiResponseFilter>();
    //services.AddTransient<LoggingActionFilter>();
    services.AddTransient<IARMExecuteAPI, ARMExecuteAPI>();
    //services.AddTransient<IARMQueue, ARMQueue>();
    services.AddTransient<IARMAppStatusV2, ARMAppStatusV2>();
    services.AddTransient<IEntityService, EntityService>();
    services.AddTransient<ITstructDataService, TstructDataService>();
    services.AddTransient<IAxPutDataService, AxPutDataService>();
    services.AddTransient<IAxGetDataService, AxGetDataService>();
    services.AddTransient<ISqlMetadataService, SqlMetadataService>();

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

    //services.AddSignalR();

    //services.AddDistributedMemoryCache();
    //services.AddSession(options =>
    //{
    //    options.IdleTimeout = TimeSpan.FromMinutes(10);
    //});

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    //services.AddEndpointsApiExplorer();
    //services.AddSwaggerGen();

    var app = builder.Build();
    //app.UseRequestLocalization();
    // Configure the HTTP request pipeline.
    //if (app.Environment.IsDevelopment())
    //{
    //    app.UseSwagger();
    //    app.UseSwaggerUI();
    //}
    app.UseStaticFiles();
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseRouting();

    //app.UseHttpsRedirection();

    // Shows UseCors with CorsPolicyBuilder.
    app.UseCors("CorsPolicy");
    app.UseAuthentication();
    //app.UseSession();
    app.UseAuthorization();

    app.Use((context, next) =>
    {
        context.Request.EnableBuffering();
        return next();
    });

    //app.Use(async (context, next) =>
    //{
    //    var token = context.Session.GetString("TokenId");
    //    if (!string.IsNullOrEmpty(token))
    //    {
    //        context.Request.Headers.Add("Authorization", "Bearer " + token);
    //    }
    //    context.Request.EnableBuffering();
    //    await next();

    //});
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        //endpoints.MapHub<NotificationHub>("/notificationHub");

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
        string source = "ARM_APIs";
        string log = "Application";
        string message = $"[{DateTime.Now}] Unhandled Exception: {ex.Message}\nStack Trace: {ex.StackTrace}";
        if (!EventLog.SourceExists(source))
        {
            EventLog.CreateEventSource(source, log);
        }
        EventLog.WriteEntry(source, message, EventLogEntryType.Error);
    }
    catch(Exception ex1)
    {
        // If logging to Event Viewer fails, we silently ignore to avoid recursive failures.
        Console.WriteLine($"[{DateTime.Now}] Unhandled Exception: {ex1.Message}\nStack Trace: {ex1.StackTrace}");
    }
}
static void LogToFile(Exception ex)
{
    try { 
        var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDir); // creates if not exists

        var logFile = Path.Combine(logDir, $"startup-error_{DateTime.Now:dd-MMM-yyyy}.txt");

        File.AppendAllText( logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}"
        );

    }
    catch (Exception ex1)
    {
        // If logging to LogToFile fails, we silently ignore to avoid recursive failures.
        Console.WriteLine($"[{DateTime.Now}] Unhandled Exception: {ex1.Message}\nStack Trace: {ex1.StackTrace}");
    }
}