using System.Security.Claims;
using System.Text;
using AxData.Api.Contracts;
using AxData.Domain.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AxData.Api.Identity;

public static class AxAuthAuthentication
{
    public static IServiceCollection AddAxAuthAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var key = Required(configuration["Jwt:Key"], "Jwt:Key");
        var issuer = Required(configuration["Jwt:Issuer"], "Jwt:Issuer");
        var audience = Required(configuration["Jwt:Audience"], "Jwt:Audience");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256Signature],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await WriteErrorAsync(
                            context.HttpContext,
                            AxDataErrors.AuthenticationRequired).ConfigureAwait(false);
                    },
                    OnForbidden = context => WriteErrorAsync(
                        context.HttpContext,
                        AxDataErrors.AccessDenied)
                };
            });
        services.AddAuthorization();
        return services;
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        AxDataErrorDefinition error)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = error.HttpStatusCode;
        await context.Response.WriteAsJsonAsync(
            AxDataErrorResponse.Create(error.Code, error.Message, context.TraceIdentifier),
            context.RequestAborted).ConfigureAwait(false);
    }

    private static string Required(string? value, string key) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException(
                $"Configuration '{key}' is required for production authentication.");
}
