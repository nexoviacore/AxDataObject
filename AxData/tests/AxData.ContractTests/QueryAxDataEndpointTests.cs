using System.Text.Json;
using AxData.Api.Contracts;
using AxData.Api.Routing;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Application.Query;
using AxData.Domain.Model;
using AxData.TestSupport.Fakes;
using AxData.TestSupport.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AxData.ContractTests;

public sealed class QueryAxDataEndpointTests
{
    [Fact]
    public async Task Hidden_field_is_absent_from_success_json()
    {
        var context = CreateHttpContext("corr-query");
        var (handler, authorization) = await CreateHandlerAsync("viewer");
        authorization.QueryPermission = FakeAxpertAuthorizationProvider.CreateQueryPermission(
            canEditTransaction: false,
            excludedFields: FakeAxpertAuthorizationProvider.FieldSets(("dc1", ["secret"])),
            allowedOperations: FakeAxpertAuthorizationProvider.Set("query"));
        using var select = JsonDocument.Parse("\"*\"");

        var result = await AxDataEndpoints.HandleQueryAsync(
            "object-1",
            new QueryAxDataRequest(select.RootElement.Clone()),
            handler,
            context,
            CancellationToken.None);

        await result.ExecuteAsync(context);
        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);
        var json = response.RootElement.GetRawText();
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Contains("customer", json, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", json, StringComparison.Ordinal);
        Assert.True(response.RootElement.GetProperty("readOnly").GetBoolean());
        Assert.Equal("dc1", response.RootElement.GetProperty("dataContainers")[0].GetProperty("dcId").GetString());
        Assert.Equal("corr-query", response.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task Unauthorized_query_uses_standard_error_envelope()
    {
        var context = CreateHttpContext("corr-denied");
        var (handler, authorization) = await CreateHandlerAsync("unrelated");
        authorization.QueryPermission = AxData.Application.Ports.Axpert.AxQueryPermission.Denied;

        var result = await AxDataEndpoints.HandleGetAsync(
            "object-1",
            handler,
            context,
            CancellationToken.None);

        await result.ExecuteAsync(context);
        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal("error", response.RootElement.GetProperty("status").GetString());
        Assert.Equal("AXD-AUTH-002", response.RootElement.GetProperty("code").GetString());
        Assert.Equal("corr-denied", response.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task StructDef_hash_mismatch_returns_safe_409_envelope()
    {
        var context = CreateHttpContext("corr-definition");
        var mismatch = SampleStructDefFixture.Definition with { Version = "changed" };
        var (handler, _) = await CreateHandlerAsync("owner", mismatch);

        var result = await AxDataEndpoints.HandleGetAsync(
            "object-1", handler, context, CancellationToken.None);

        await result.ExecuteAsync(context);
        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("AXD-CONC-003", response.RootElement.GetProperty("code").GetString());
        Assert.Equal("corr-definition", response.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public void Selection_document_maps_to_application_command()
    {
        using var document = JsonDocument.Parse("""
            [{"dcId":"dc1","fields":["customer"]},{"dcId":"dc2","fields":["*"]}]
            """);
        var request = new QueryAxDataRequest(document.RootElement.Clone());

        var mapped = request.TryToCommand("object-1", out var command);

        Assert.True(mapped);
        Assert.False(command.SelectAll);
        Assert.Equal(2, command.Selections.Count);
        Assert.Equal("customer", command.Selections[0].Fields[0]);
    }

    private static async Task<(QueryAxDataHandler Handler, FakeAxpertAuthorizationProvider Authorization)> CreateHandlerAsync(
        string userName,
        StructDefHandle? currentDefinition = null)
    {
        var state = SampleStructDefFixture.CreateState();
        state.DataContainers["dc1"].Rows[0].Fields.Add("secret", new AxFieldValue("classified"));
        var store = new InMemoryAxDataStore();
        await store.WriteAsync(state, null, CancellationToken.None);
        var authorization = new FakeAxpertAuthorizationProvider();
        var definitionProvider = new FakeStructDefProvider(currentDefinition ?? SampleStructDefFixture.Definition);
        return (new QueryAxDataHandler(
            new FakeUserContext(new AxUserContext(
                SampleStructDefFixture.TenantId,
                userName,
                new HashSet<string>())),
            store,
            definitionProvider,
            authorization,
            FakeRecovery.Create(store, authorization, definitionProvider)), authorization);
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
}
