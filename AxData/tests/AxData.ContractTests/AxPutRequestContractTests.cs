using System.Text.Json;
using AxData.Contracts.AxPut;
using AxTstructData;

namespace AxData.ContractTests;

public sealed class AxPutRequestContractTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Missing_objectid_generates_ulid_and_defaults_retention_true()
    {
        var request = new AxPutObjectRequestContract { RetainDataObject = null };

        var resolved = AxPutObjectContractRules.Resolve(request, dataWasSupplied: true, dataCount: 1, Now);

        Assert.False(resolved.ObjectIdWasSupplied);
        Assert.Matches("^[0-7][0-9A-HJKMNP-TV-Z]{25}$", resolved.ObjectId);
        Assert.True(resolved.RetainDataObject);
    }

    [Fact]
    public void Generated_objectid_requires_nonempty_data()
    {
        var request = new AxPutObjectRequestContract();
        Assert.Throws<ArgumentException>(() =>
            AxPutObjectContractRules.Resolve(request, dataWasSupplied: false, dataCount: 0, Now));
        Assert.Throws<ArgumentException>(() =>
            AxPutObjectContractRules.Resolve(request, dataWasSupplied: true, dataCount: 0, Now));
    }

    [Fact]
    public void Existing_objectid_allows_omitted_data()
    {
        var request = new AxPutObjectRequestContract
        {
            ObjectId = " 01JEXISTINGOBJECT0000000000 ",
            RetainDataObject = false
        };

        var resolved = AxPutObjectContractRules.Resolve(
            request,
            dataWasSupplied: false,
            dataCount: 0,
            Now);

        Assert.Equal("01JEXISTINGOBJECT0000000000", resolved.ObjectId);
        Assert.True(resolved.ObjectIdWasSupplied);
        Assert.False(resolved.RetainDataObject);
    }

    [Fact]
    public void Existing_objectid_rejects_explicit_empty_data()
    {
        var request = new AxPutObjectRequestContract { ObjectId = "01JEXISTINGOBJECT0000000000" };
        Assert.Throws<ArgumentException>(() =>
            AxPutObjectContractRules.Resolve(request, dataWasSupplied: true, dataCount: 0, Now));
    }

    [Fact]
    public void Missing_and_null_submitData_are_valid_wire_shapes()
    {
        const string missing = "{\"ARMSessionId\":\"session\",\"Data\":[{\"TransId\":\"sales\",\"Action\":\"create\"}]}";
        const string explicitNull = "{\"ARMSessionId\":\"session\",\"Data\":[{\"TransId\":\"sales\",\"Action\":\"create\",\"SubmitData\":null}]}";

        var missingRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<AxPutInput>(missing)!;
        var nullRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<AxPutInput>(explicitNull)!;

        Assert.Empty(missingRequest.Data!.Single().SubmitData);
        Assert.Null(nullRequest.Data!.Single().SubmitData);
        Assert.NotNull(AxPutObjectContractRules.Resolve(missingRequest, true, 1, Now));
        Assert.NotNull(AxPutObjectContractRules.Resolve(nullRequest, true, 1, Now));
    }

    [Fact]
    public void AxPut_wire_model_binds_objectid_and_retention()
    {
        const string json = "{\"ARMSessionId\":\"session\",\"objectid\":\"01JEXISTING\",\"retainDataObject\":false}";

        var request = Newtonsoft.Json.JsonConvert.DeserializeObject<AxPutInput>(json)!;

        Assert.Equal("01JEXISTING", request.ObjectId);
        Assert.False(request.RetainDataObject);
        Assert.Null(request.Data);
    }

    [Fact]
    public void Client_cannot_bind_schema_or_StructDef_bypass_controls()
    {
        const string json = "{\"ARMSessionId\":\"session\",\"RequestTraceId\":\"client-trace\",\"bypassSchemaValidation\":true,\"structDefVersion\":\"client-version\"}";

        var request = Newtonsoft.Json.JsonConvert.DeserializeObject<AxPutInput>(json)!;

        Assert.Null(request.RequestTraceId);
        Assert.DoesNotContain(
            typeof(AxPutInput).GetProperties(),
            property => property.Name.Contains("bypass", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            typeof(AxPutInput).GetProperties(),
            property => property.Name.Contains("structdef", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Response_omits_object_version()
    {
        var json = JsonSerializer.Serialize(new AxPutObjectResponseContract("01JOBJECT", null));

        Assert.Contains("\"objectid\":\"01JOBJECT\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("version", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dataObjectRetained", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Retention_uses_the_AxPut_response_name()
    {
        var json = JsonSerializer.Serialize(new AxPutObjectResponseContract("01JOBJECT", true));

        Assert.Contains("\"objectid\":\"01JOBJECT\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("version", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"dataObjectRetained\":true", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Pending_finalization_is_explicit_and_omitted_from_normal_results()
    {
        var pending = JsonSerializer.Serialize(new AxPutObjectResponseContract("01JOBJECT", true, true));
        var normal = JsonSerializer.Serialize(new AxPutObjectResponseContract("01JOBJECT", true));

        Assert.Contains("\"dataObjectFinalizationPending\":true", pending, StringComparison.Ordinal);
        Assert.DoesNotContain("dataObjectFinalizationPending", normal, StringComparison.Ordinal);
    }
}
