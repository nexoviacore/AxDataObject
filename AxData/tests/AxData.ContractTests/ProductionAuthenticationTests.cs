using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AxData.Api.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AxData.ContractTests;

public sealed class ProductionAuthenticationTests
{
    private const string Key = "0123456789abcdef0123456789abcdef";
    private const string Issuer = "axauth";
    private const string Audience = "axdata";

    [Fact]
    public void Valid_AxAuth_token_uses_pinned_algorithm_and_thirty_second_skew()
    {
        var parameters = ValidationParameters();
        var token = Token(Issuer, Audience, SecurityAlgorithms.HmacSha256Signature);

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, parameters, out var validated);

        Assert.Equal("reviewer", principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal(SecurityAlgorithms.HmacSha256Signature, ((JwtSecurityToken)validated).Header.Alg);
        Assert.Equal(TimeSpan.FromSeconds(30), parameters.ClockSkew);
        Assert.Equal([SecurityAlgorithms.HmacSha256Signature], parameters.ValidAlgorithms);
    }

    [Theory]
    [InlineData("wrong-issuer", Audience)]
    [InlineData(Issuer, "wrong-audience")]
    public void Wrong_issuer_or_audience_is_rejected(string issuer, string audience)
    {
        var token = Token(issuer, audience, SecurityAlgorithms.HmacSha256Signature);

        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, ValidationParameters(), out _));
    }

    [Fact]
    public void Expired_token_is_rejected()
    {
        var token = Token(
            Issuer,
            Audience,
            SecurityAlgorithms.HmacSha256Signature,
            DateTime.UtcNow.AddMinutes(-2));

        Assert.Throws<SecurityTokenExpiredException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, ValidationParameters(), out _));
    }

    [Fact]
    public void Algorithm_tampering_is_rejected()
    {
        var token = Token(Issuer, Audience, SecurityAlgorithms.HmacSha256);

        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, ValidationParameters(), out _));
    }

    [Fact]
    public async Task Body_session_disagreement_returns_authentication_error()
    {
        var reachedNext = false;
        var middleware = new ArmSessionConsistencyMiddleware(_ =>
        {
            reachedNext = true;
            return Task.CompletedTask;
        });
        var context = Context("ARM-app-token", "ARM-app-body");

        await middleware.InvokeAsync(context);

        Assert.False(reachedNext);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("AXD-AUTH-001", response.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Matching_transitional_body_session_is_ignored_and_body_is_rewound()
    {
        var reachedNext = false;
        var middleware = new ArmSessionConsistencyMiddleware(async context =>
        {
            reachedNext = true;
            using var body = await JsonDocument.ParseAsync(context.Request.Body);
            Assert.Equal("sample", body.RootElement.GetProperty("transId").GetString());
        });
        var context = Context("ARM-app-session", "ARM-app-session");

        await middleware.InvokeAsync(context);

        Assert.True(reachedNext);
    }

    private static TokenValidationParameters ValidationParameters()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAxAuthAuthentication(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:Key"] = Key,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience
            }).Build());
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme)
            .TokenValidationParameters;
    }

    private static string Token(
        string issuer,
        string audience,
        string algorithm,
        DateTime? expires = null)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
            algorithm);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            [
                new Claim(ClaimTypes.Name, "reviewer"),
                new Claim(HttpUserContextClaims.ArmSessionId, "ARM-app-session"),
                new Claim(ClaimTypes.NameIdentifier, "ARM-app-session")
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: expires ?? DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static DefaultHttpContext Context(string tokenSessionId, string bodySessionId)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(new
        {
            transId = "sample",
            ARMSessionId = bodySessionId
        });
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(HttpUserContextClaims.ArmSessionId, tokenSessionId)],
                JwtBearerDefaults.AuthenticationScheme)),
            Request =
            {
                Body = new MemoryStream(body),
                ContentLength = body.Length,
                ContentType = "application/json"
            },
            Response = { Body = new MemoryStream() }
        };
    }
}
