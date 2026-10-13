using System.Text.Json;
using AxData.Api.Contracts;
using AxData.Api.Routing;
using AxData.Application.Init;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.TestSupport.Fakes;
using AxData.TestSupport.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AxData.ContractTests;

public sealed class InitializeAxDataEndpointTests
{
    [Fact]
    public async Task Success_response_matches_initialize_contract()
    {
        var context = CreateHttpContext("corr-init");
        var result = await AxDataEndpoints.HandleInitializeAsync(
            new InitializeAxDataRequest(SampleStructDefFixture.TransId, null),
            CreateHandler(),
            context,
            CancellationToken.None);

        await result.ExecuteAsync(context);
        var response = await ReadAsync<InitializeAxDataApiResponse>(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
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
            new InMemoryAxDataStore(),
            new FakeObjectIdGenerator("object-http"),
            new FakeClock(DateTimeOffset.Parse("2026-09-25T08:00:00Z")),
            new InitializeAxDataOptions());
    }

    private static DefaultHttpContext CreateHttpContext(string correlationId)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = correlationId,
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<T> ReadAsync<T>(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        return (await JsonSerializer.DeserializeAsync<T>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;
    }
}
