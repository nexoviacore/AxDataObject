using ARMCommon.ActionFilter;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Services;
using ARMNotificationHub;
using ARMNotificationHub.Helper;
using ARMNotificationHub.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var _configuration = builder.Configuration;

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        var allowedDomains = builder.Configuration
            .GetSection("AllowedDomains")?
            .Get<string[]>()?
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .ToArray();

        if (allowedDomains != null && allowedDomains.Length > 0)
        {
            policy
                .WithOrigins(allowedDomains)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();
builder.Services.AddSingleton<IAppSettingsHelper, AppSettingsHelper>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddScoped<Utils>();
builder.Services.AddScoped<IRedisCacheHelper, RedisCacheHelper>();
builder.Services.AddScoped<RedisNotificationHelper>();
builder.Services.AddScoped<IARMLogTraceService, ARMLogTraceService>();
builder.Services.AddScoped<IRedisHelper, RedisHelper>();

builder.Services.AddTransient<IARMNotificationService, ARMNotificationService>();
builder.Services.AddTransient<ApiResponseFilter>();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(2, 1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddAuthorization();

builder.Services.AddVersionedApiExplorer(
    options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;

    });

builder.Services.AddAuthentication(auth =>
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


// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseRouting();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<NotificationHub>("/notificationHub");
});

app.Run();
