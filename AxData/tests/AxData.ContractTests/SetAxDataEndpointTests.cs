using System.Text.Json;
using AxData.Api.Contracts;
using AxData.Api.Controllers;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Application.Set;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;
using AxData.TestSupport.Fakes;
using AxData.TestSupport.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AxData.ContractTests;

public sealed class SetAxDataEndpointTests
{
    [Fact]
    public async Task Set_returns_success_contract_without_object_version()
    {
        var context = CreateHttpContext("corr-set");
        var handler = await CreateHandlerAsync();
        var request = Request();

        var result = await CreateController(context).SetAsync(
            request, handler, CancellationToken.None);

        using var response = ToJsonDocument(Assert.IsType<OkObjectResult>(result));
        Assert.Equal("success", response.RootElement.GetProperty("status").GetString());
        Assert.False(response.RootElement.TryGetProperty("version", out _));
        Assert.Equal("corr-set", response.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task Validation_errors_return_completed_validation_error_contract()
    {
        const string json = """
            {"objectId":"object-1","getDeps":true,
             "data":{"dc2":{"rows":[{"rowId":"row_existing","action":"e","fields":{"qty":"not-a-number"}}]}}}
            """;
        var request = JsonSerializer.Deserialize<SetAxDataRequest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
        var context = CreateHttpContext("corr-validation");
        var handler = await CreateHandlerAsync();

        var result = await CreateController(context).SetAsync(
            request, handler, CancellationToken.None);

        using var response = ToJsonDocument(Assert.IsType<OkObjectResult>(result));
        Assert.Equal("validation_error", response.RootElement.GetProperty("status").GetString());
        Assert.False(response.RootElement.TryGetProperty("version", out _));
        Assert.Empty(response.RootElement.GetProperty("changed").EnumerateArray());
        Assert.Equal("INVALID_NUMBER", response.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
        Assert.Equal("dc2", response.RootElement.GetProperty("errors")[0].GetProperty("address").GetProperty("dcId").GetString());
    }

    [Fact]
    public void Request_maps_dc_rows_actions_and_primitive_values()
    {
        var request = Request();

        var mapped = request.TryToCommand(out var command);

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
            {"objectId":"object-1","getDeps":false,"bypassPermission":true,
             "data":{"dc2":{"rows":[{"rowId":"row_existing","action":"e","fields":{"qty":2}}]}}}
            """;
        var request = JsonSerializer.Deserialize<SetAxDataRequest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
        var context = CreateHttpContext("corr-bypass");
        var handler = await CreateHandlerAsync("unrelated");

        var result = await CreateController(context).SetAsync(
            request, handler, CancellationToken.None);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task New_row_omits_row_id_and_receives_server_generated_identity()
    {
        const string json = """
            {"objectId":"object-1","getDeps":false,
             "data":{"dc2":{"rows":[{"action":"n","fields":{"qty":2,"rate":5}}]}}}
            """;
        var request = JsonSerializer.Deserialize<SetAxDataRequest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
        var context = CreateHttpContext("corr-new-row");
        var handler = await CreateHandlerAsync();

        var result = await CreateController(context).SetAsync(
            request, handler, CancellationToken.None);

        using var response = ToJsonDocument(Assert.IsType<OkObjectResult>(result));
        var created = Assert.Single(response.RootElement.GetProperty("createdRows").EnumerateArray());
        Assert.Equal("dc2", created.GetProperty("dcId").GetString());
        Assert.Equal(0, created.GetProperty("requestRowIndex").GetInt32());
        Assert.Equal("row_fake_1", created.GetProperty("rowId").GetString());
    }

    [Fact]
    public async Task Record_id_and_row_number_cannot_address_an_edit()
    {
        const string json = """
            {"objectId":"object-1","getDeps":false,
             "data":{"dc2":{"rows":[{"recordId":10,"rowNo":1,"action":"e","fields":{"qty":2}}]}}}
            """;
        var request = JsonSerializer.Deserialize<SetAxDataRequest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
        var context = CreateHttpContext("corr-record-id");
        var handler = await CreateHandlerAsync();

        var result = await CreateController(context).SetAsync(
            request, handler, CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        using var response = ToJsonDocument(error);
        Assert.Equal(StatusCodes.Status400BadRequest, error.StatusCode);
        Assert.Equal("AXD-REQ-001", response.RootElement.GetProperty("code").GetString());
    }

    private static SetAxDataRequest Request()
    {
        using var document = JsonDocument.Parse("2");
        return new SetAxDataRequest(
            "object-1",
            false,
            new Dictionary<string, SetDataContainerRequest>
            {
                ["dc2"] = new(
                [new SetRowRequest("row_existing", "e", new Dictionary<string, JsonElement>
                {
                    ["qty"] = document.RootElement.Clone()
                })])
            });
    }

    [Fact]
    public void Blank_object_id_is_rejected()
    {
        var request = Request() with { ObjectId = " " };

        Assert.False(request.TryToCommand(out _));
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
                ["qty"] = new(1m, isSourceKey: true),
                ["rate"] = new(4m),
                ["amount"] = new(4m)
            }));
        var store = new InMemoryAxDataStore();
        await store.WriteAsync(state, null, CancellationToken.None);
        var authorization = new FakeAxpertAuthorizationProvider();
        var fillGridEngine = new FakeAxpertFillGridEngine
        {
            Result = new(true, [new(true, "dc2", FillGridMode.Append, [])])
        };
        return new SetAxDataHandler(
            new FakeUserContext(new AxUserContext(
                SampleStructDefFixture.TenantId,
                userName,
                new HashSet<string>())),
            store,
            new FakeAxDataLock(),
            authorization,
            new FakeAxpertFieldEngine(
                currentDefinition ?? SampleStructDefFixture.Definition,
                fillGridEngine),
            new FakeRowIdGenerator());
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

    private static JsonDocument ToJsonDocument(ObjectResult result) =>
        JsonDocument.Parse(JsonSerializer.Serialize(
            result.Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
}
