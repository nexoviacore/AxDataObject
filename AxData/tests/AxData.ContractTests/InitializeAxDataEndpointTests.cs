using AxData.Api.Controllers;
using AxData.Api.Contracts;
using AxData.Application.Init;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.TestSupport.Fakes;
using AxData.TestSupport.Fixtures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AxData.ContractTests;

public sealed class InitializeAxDataEndpointTests
{
    [Fact]
    public void Controller_preserves_routes_and_requires_authorization()
    {
        var controller = typeof(AxDataController);

        Assert.NotNull(controller.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true).SingleOrDefault());
        Assert.NotNull(controller.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
        Assert.Equal(
            "/api/v1/axdata",
            Assert.Single(controller.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>()).Template);
        Assert.Null(GetActionAttribute<HttpPostAttribute>(nameof(AxDataController.InitializeAsync)).Template);
        Assert.Equal("{objectId}/query", GetActionAttribute<HttpPostAttribute>(nameof(AxDataController.QueryAsync)).Template);
        Assert.Equal("{objectId}/set", GetActionAttribute<HttpPostAttribute>(nameof(AxDataController.SetAsync)).Template);
        Assert.Equal("{objectId}", GetActionAttribute<HttpGetAttribute>(nameof(AxDataController.GetAsync)).Template);
    }

    [Fact]
    public async Task Success_response_matches_initialize_contract()
    {
        var context = CreateHttpContext("corr-init");
        var result = await CreateController(context).InitializeAsync(
            new InitializeAxDataRequest(SampleStructDefFixture.TransId, null),
            CreateHandler(),
            CancellationToken.None);

        var response = Assert.IsType<InitializeAxDataApiResponse>(
            Assert.IsType<OkObjectResult>(result).Value);

        Assert.Equal("success", response.Status);
        Assert.Equal("object-http", response.ObjectId);
        Assert.Equal(1, response.Version);
        Assert.Equal("corr-init", response.CorrelationId);
    }

    [Fact]
    public void Existing_key_contract_maps_field_and_value()
    {
        var request = new InitializeAxDataRequest(
            "salesorder",
            new InitializeAxDataKeyRequest("recordid", "100501"));

        var command = request.ToCommand();

        Assert.Equal("recordid", command.Key!.KeyField);
        Assert.Equal("100501", command.Key.KeyValue);
        Assert.Null(command.Key.RecordId);
    }

    private static InitializeAxDataHandler CreateHandler()
    {
        var loader = new FakeAxpertDataLoader
        {
            Result = new AxDataLoadResult(true, SampleStructDefFixture.CreateState(), null)
        };
        return new InitializeAxDataHandler(
            new FakeUserContext(new AxUserContext(
                SampleStructDefFixture.TenantId,
                "owner",
                new HashSet<string>())),
            new FakeAxpertAuthorizationProvider(),
            new FakeStructDefProvider(SampleStructDefFixture.Definition),
            loader,
            new FakeAxpertFieldEngine(SampleStructDefFixture.Definition),
            new InMemoryAxDataStore(),
            new FakeObjectIdGenerator("object-http"),
            new FakeClock(DateTimeOffset.Parse("2026-09-25T08:00:00Z")),
            new InitializeAxDataOptions());
    }

    private static DefaultHttpContext CreateHttpContext(string correlationId)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = correlationId
        };
        return context;
    }

    private static AxDataController CreateController(HttpContext context) =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

    private static TAttribute GetActionAttribute<TAttribute>(string methodName)
        where TAttribute : Attribute =>
        Assert.Single(typeof(AxDataController)
            .GetMethod(methodName)!
            .GetCustomAttributes(typeof(TAttribute), inherit: true)
            .Cast<TAttribute>());
}
