using AxData.Api.Identity;
using AxData.Api.Middleware;
using AxData.Api.Routing;
using AxData.Application.Ports;
using AxData.Application.Init;
using AxData.Application.Query;
using AxData.Application.Recovery;
using AxData.Application.Set;
using AxData.Api.Development;
using AxData.Api.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();
builder.Services.AddScoped<InitializeAxDataHandler>();
builder.Services.AddScoped<QueryAxDataHandler>();
builder.Services.AddScoped<SetAxDataHandler>();
builder.Services.AddScoped<AxDataRecoveryService>();
builder.Services.AddSingleton(new InitializeAxDataOptions());
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IObjectIdGenerator, CryptographicObjectIdGenerator>();
builder.Services.AddSingleton<IRowIdGenerator, CryptographicRowIdGenerator>();
builder.Services.AddSingleton(OperationalLoggingOptions.FromConfiguration(builder.Configuration));
builder.Services.AddSingleton<IOperationalEventSink, MicrosoftOperationalEventSink>();
var m1DevelopmentMode = builder.Services.AddM1DevelopmentMode(builder.Configuration, builder.Environment);
var realDataReviewMode = builder.Services.AddRealDataReviewMode(builder.Configuration, builder.Environment);

if (m1DevelopmentMode && realDataReviewMode)
{
    throw new InvalidOperationException("AxData M1 and RealDataReview modes cannot both be enabled.");
}

var app = builder.Build();

if (m1DevelopmentMode || realDataReviewMode)
{
    app.UseMiddleware<DevelopmentIdentityMiddleware>();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestCompletionLoggingMiddleware>();
app.UseMiddleware<ApiExceptionMiddleware>();

app.MapGet("/health", () => Results.Ok());
app.MapGroup(ApiRoutes.BasePath).MapAxDataEndpoints();

app.Run();

public partial class Program;
