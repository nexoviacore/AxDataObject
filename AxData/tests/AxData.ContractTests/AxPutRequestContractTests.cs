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

        var resolved = AxPutObjectContractRules.Resolve(request, null, dataWasSupplied: true, dataCount: 1, Now);

        Assert.False(resolved.ObjectIdWasSupplied);
        Assert.Matches("^[0-7][0-9A-HJKMNP-TV-Z]{25}$", resolved.ObjectId);
        Assert.True(resolved.RetainDataObject);
        Assert.Null(resolved.ExpectedVersion);
    }

    [Fact]
    public void Generated_objectid_requires_nonempty_data_and_rejects_version_metadata()
    {
        var request = new AxPutObjectRequestContract();
        Assert.Throws<ArgumentException>(() =>
            AxPutObjectContractRules.Resolve(request, null, dataWasSupplied: false, dataCount: 0, Now));
        Assert.Throws<ArgumentException>(() =>
            AxPutObjectContractRules.Resolve(request, null, dataWasSupplied: true, dataCount: 0, Now));

        request.ExpectedVersion = 1;
        Assert.Throws<ArgumentException>(() =>
            AxPutObjectContractRules.Resolve(request, null, dataWasSupplied: true, dataCount: 1, Now));
    }

    [Fact]
    public void Existing_objectid_allows_omitted_data_and_resolves_matching_versions()
    {
        var request = new AxPutObjectRequestContract
        {
            ObjectId = " 01JEXISTINGOBJECT0000000000 ",
            RetainDataObject = false,
            ExpectedVersion = 14
        };

        var resolved = AxPutObjectContractRules.Resolve(
            request,
            "\"14\"",
            dataWasSupplied: false,
            dataCount: 0,
            Now);

        Assert.Equal("01JEXISTINGOBJECT0000000000", resolved.ObjectId);
        Assert.True(resolved.ObjectIdWasSupplied);
        Assert.False(resolved.RetainDataObject);
        Assert.Equal(14, resolved.ExpectedVersion);
    }

    [Fact]
    public void Existing_objectid_accepts_header_only_or_body_only_version()
    {
        var headerOnly = AxPutObjectContractRules.Resolve(
            new AxPutObjectRequestContract { ObjectId = "01JHEADER" },
            "\"7\"",
            dataWasSupplied: true,
            dataCount: 1,
            Now);
        var bodyOnly = AxPutObjectContractRules.Resolve(
            new AxPutObjectRequestContract { ObjectId = "01JBODY", ExpectedVersion = 8 },
            null,
            dataWasSupplied: true,
            dataCount: 1,
            Now);

        Assert.Equal(7, headerOnly.ExpectedVersion);
        Assert.Equal(8, bodyOnly.ExpectedVersion);
    }

    [Fact]
    public void Existing_objectid_rejects_empty_data_missing_version_and_version_disagreement()
    {
        var request = new AxPutObjectRequestContract { ObjectId = "01JEXISTINGOBJECT0000000000" };
        Assert.Throws<ArgumentException>(() =>
            AxPutObjectContractRules.Resolve(request, "\"14\"", dataWasSupplied: true, dataCount: 0, Now));
        Assert.Throws<ArgumentException>(() =>
            AxPutObjectContractRules.Resolve(request, null, dataWasSupplied: false, dataCount: 0, Now));

        request.ExpectedVersion = 13;
        Assert.Throws<ArgumentException>(() =>
            AxPutObjectContractRules.Resolve(request, "\"14\"", dataWasSupplied: false, dataCount: 0, Now));
        Assert.Throws<ArgumentException>(() =>
            AxPutObjectContractRules.Resolve(request, "14", dataWasSupplied: false, dataCount: 0, Now));
        Assert.Throws<ArgumentException>(() =>
            AxPutObjectContractRules.Resolve(request, "\"14\",\"15\"", dataWasSupplied: false, dataCount: 0, Now));
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
        Assert.NotNull(AxPutObjectContractRules.Resolve(missingRequest, null, true, 1, Now));
        Assert.NotNull(AxPutObjectContractRules.Resolve(nullRequest, null, true, 1, Now));
    }

    [Fact]
    public void AxPut_wire_model_binds_objectid_retention_and_expected_version()
    {
        const string json = "{\"ARMSessionId\":\"session\",\"objectid\":\"01JEXISTING\",\"retainDataObject\":false,\"expectedVersion\":9}";

        var request = Newtonsoft.Json.JsonConvert.DeserializeObject<AxPutInput>(json)!;

        Assert.Equal("01JEXISTING", request.ObjectId);
        Assert.False(request.RetainDataObject);
        Assert.Equal(9, request.ExpectedVersion);
        Assert.Null(request.Data);
    }

    [Fact]
    public void Unknown_version_is_omitted_instead_of_serialized_as_null()
    {
        var json = JsonSerializer.Serialize(new AxPutObjectResponseContract("01JOBJECT", null, null));

        Assert.Contains("\"objectid\":\"01JOBJECT\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("version", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dataObjectRetained", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Known_version_and_retention_use_the_AxPut_response_names()
    {
        var json = JsonSerializer.Serialize(new AxPutObjectResponseContract("01JOBJECT", 14, true));

        Assert.Contains("\"objectid\":\"01JOBJECT\"", json, StringComparison.Ordinal);
        Assert.Contains("\"version\":14", json, StringComparison.Ordinal);
        Assert.Contains("\"dataObjectRetained\":true", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Pending_finalization_is_explicit_and_omitted_from_normal_results()
    {
        var pending = JsonSerializer.Serialize(new AxPutObjectResponseContract("01JOBJECT", 15, true, true));
        var normal = JsonSerializer.Serialize(new AxPutObjectResponseContract("01JOBJECT", 15, true));

        Assert.Contains("\"dataObjectFinalizationPending\":true", pending, StringComparison.Ordinal);
        Assert.DoesNotContain("dataObjectFinalizationPending", normal, StringComparison.Ordinal);
    }
}
