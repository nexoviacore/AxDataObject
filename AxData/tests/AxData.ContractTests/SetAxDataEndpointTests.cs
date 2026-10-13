using System.Text.Json;
using AxData.Api.Contracts;
using AxData.Api.Routing;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Application.Set;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;
using AxData.TestSupport.Fakes;
using AxData.TestSupport.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AxData.ContractTests;

public sealed class SetAxDataEndpointTests
{
    [Fact]
    public async Task If_match_request_returns_success_contract_with_incremented_version()
    {
        var context = CreateHttpContext("corr-set");
        context.Request.Headers.IfMatch = "\"1\"";
        var handler = await CreateHandlerAsync();
        var request = Request(expectedVersion: null);

        var result = await AxDataEndpoints.HandleSetAsync(
            "object-1", request, handler, context, CancellationToken.None);

        await result.ExecuteAsync(context);
        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("success", response.RootElement.GetProperty("status").GetString());
        Assert.Equal(2, response.RootElement.GetProperty("version").GetInt64());
        Assert.Equal("corr-set", response.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task Stale_body_version_returns_409_with_current_version()
    {
        var context = CreateHttpContext("corr-stale");
        var handler = await CreateHandlerAsync();

        var result = await AxDataEndpoints.HandleSetAsync(
            "object-1", Request(expectedVersion: 9), handler, context, CancellationToken.None);

        await result.ExecuteAsync(context);
        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("AXD-CONC-001", response.RootElement.GetProperty("code").GetString());
        Assert.Equal(1, response.RootElement.GetProperty("currentVersion").GetInt64());
    }

    [Fact]
    public async Task StructDef_hash_mismatch_returns_safe_409_envelope()
    {
        var context = CreateHttpContext("corr-definition");
        var mismatch = SampleStructDefFixture.Definition with { Version = "changed" };
        var handler = await CreateHandlerAsync(currentDefinition: mismatch);

        var result = await AxDataEndpoints.HandleSetAsync(
            "object-1", Request(expectedVersion: 1), handler, context, CancellationToken.None);

        await result.ExecuteAsync(context);
        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("AXD-CONC-003", response.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Validation_errors_return_completed_validation_error_contract()
    {
        const string json = """
            {"getDeps":true,"expectedVersion":1,
             "data":{"dc2":{"rows":[{"rowId":"row_existing","action":"e","fields":{"qty":"not-a-number"}}]}}}
            """;
        var request = JsonSerializer.Deserialize<SetAxDataRequest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
        var context = CreateHttpContext("corr-validation");
        var handler = await CreateHandlerAsync();

        var result = await AxDataEndpoints.HandleSetAsync(
            "object-1", request, handler, context, CancellationToken.None);

        await result.ExecuteAsync(context);
        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("validation_error", response.RootElement.GetProperty("status").GetString());
        Assert.Equal(1, response.RootElement.GetProperty("version").GetInt64());
        Assert.Empty(response.RootElement.GetProperty("changed").EnumerateArray());
        Assert.Equal("INVALID_NUMBER", response.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
        Assert.Equal("dc2", response.RootElement.GetProperty("errors")[0].GetProperty("address").GetProperty("dcId").GetString());
    }

    [Fact]
    public void Request_maps_dc_rows_actions_and_primitive_values()
    {
        var request = Request(expectedVersion: 1);

        var mapped = request.TryToCommand("object-1", 1, out var command);

        Assert.True(mapped);
        var container = Assert.Single(command.DataContainers);
        var row = Assert.Single(container.Rows);
        Assert.Equal("dc2", container.Name);
        Assert.Equal("row_existing", row.RowId);
        Assert.Equal("e", row.Action);
        Assert.Equal(2L, Assert.Single(row.Fields).Value);
    }

    [Fact]
    public async Task Client_bypass_property_cannot_authorize_set()
    {
        const string json = """
            {"getDeps":false,"expectedVersion":1,"bypassPermission":true,
             "data":{"dc2":{"rows":[{"rowId":"row_existing","action":"e","fields":{"qty":2}}]}}}
            """;
        var request = JsonSerializer.Deserialize<SetAxDataRequest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
        var context = CreateHttpContext("corr-bypass");
        var handler = await CreateHandlerAsync("unrelated");

        var result = await AxDataEndpoints.HandleSetAsync(
            "object-1", request, handler, context, CancellationToken.None);

        await result.ExecuteAsync(context);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task New_row_omits_row_id_and_receives_server_generated_identity()
    {
        const string json = """
            {"getDeps":false,"expectedVersion":1,
             "data":{"dc2":{"rows":[{"action":"n","fields":{"qty":2,"rate":5}}]}}}
            """;
        var request = JsonSerializer.Deserialize<SetAxDataRequest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
        var context = CreateHttpContext("corr-new-row");
        var handler = await CreateHandlerAsync();

        var result = await AxDataEndpoints.HandleSetAsync(
            "object-1", request, handler, context, CancellationToken.None);

        await result.ExecuteAsync(context);
        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        var created = Assert.Single(response.RootElement.GetProperty("createdRows").EnumerateArray());
        Assert.Equal("dc2", created.GetProperty("dcId").GetString());
        Assert.Equal(0, created.GetProperty("requestRowIndex").GetInt32());
        Assert.Equal("row_fake_1", created.GetProperty("rowId").GetString());
    }

    [Fact]
    public async Task Record_id_and_row_number_cannot_address_an_edit()
    {
        const string json = """
            {"getDeps":false,"expectedVersion":1,
             "data":{"dc2":{"rows":[{"recordId":10,"rowNo":1,"action":"e","fields":{"qty":2}}]}}}
            """;
        var request = JsonSerializer.Deserialize<SetAxDataRequest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
        var context = CreateHttpContext("corr-record-id");
        var handler = await CreateHandlerAsync();

        var result = await AxDataEndpoints.HandleSetAsync(
            "object-1", request, handler, context, CancellationToken.None);

        await result.ExecuteAsync(context);
        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("AXD-REQ-001", response.RootElement.GetProperty("code").GetString());
    }

    private static SetAxDataRequest Request(long? expectedVersion)
    {
        using var document = JsonDocument.Parse("2");
        return new SetAxDataRequest(
            false,
            expectedVersion,
            new Dictionary<string, SetDataContainerRequest>
            {
                ["dc2"] = new(
                [new SetRowRequest("row_existing", "e", new Dictionary<string, JsonElement>
                {
                    ["qty"] = document.RootElement.Clone()
                })])
            });
    }

    private static async Task<SetAxDataHandler> CreateHandlerAsync(
        string userName = "owner",
        StructDefHandle? currentDefinition = null)
    {
        var state = SampleStructDefFixture.CreateState();
        state.DataContainers["dc2"].AddRow(new AxDataRow(
            "row_existing",
            AxRowState.Unchanged,
            10,
            new Dictionary<string, AxFieldValue>
            {
                ["item"] = new("persisted"),
                ["qty"] = new(1m),
                ["rate"] = new(4m),
                ["amount"] = new(4m)
            }));
        var store = new InMemoryAxDataStore();
        await store.WriteAsync(state, null, CancellationToken.None);
        var definitionProvider = new FakeStructDefProvider(currentDefinition ?? SampleStructDefFixture.Definition);
        var authorization = new FakeAxpertAuthorizationProvider();
        return new SetAxDataHandler(
            new FakeUserContext(new AxUserContext(
                SampleStructDefFixture.TenantId,
                userName,
                new HashSet<string>())),
            store,
            new FakeAxDataLock(),
            definitionProvider,
            authorization,
            new FakeAxpertFieldEngine(SampleStructDefFixture.Definition),
            new FakeAxpertFillGridEngine
            {
                Result = new(true, [new(true, "dc2", FillGridMode.Append, [])])
            },
            new InMemorySnapshotTransport(store),
            new FakeRowIdGenerator(),
            new FakeClock(DateTimeOffset.UtcNow),
            FakeRecovery.Create(store, authorization, definitionProvider));
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
