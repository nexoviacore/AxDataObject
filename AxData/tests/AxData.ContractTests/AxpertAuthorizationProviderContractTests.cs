using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Integration.Axpert.Authorization;
using AxData.Integration.Axpert.StructDef;

namespace AxData.ContractTests;

public sealed class AxpertAuthorizationProviderContractTests
{
    private static readonly AxUserContext User = new(
        "tenant-a",
        "reviewer",
        new HashSet<string>(["role-a"], StringComparer.OrdinalIgnoreCase));

    [Fact]
    public async Task User_create_row_overrides_allowing_role_row()
    {
        var source = new FakePermissionSource();
        source.Results[AxpertPermissionKind.Create] = Success(
            Row(permissionType: "Role", fullControl: "T", access: "YES"),
            Row(permissionType: "User", access: "NO"));
        var provider = CreateProvider(source);

        var permission = await provider.GetCreatePermissionAsync(
            "tenant-a", "dtchk", User, default);

        Assert.False(permission.IsAllowed);
    }

    [Fact]
    public async Task View_permission_omits_masked_fields_and_marks_noneditable_nodes_read_only()
    {
        var source = new FakePermissionSource();
        source.Results[AxpertPermissionKind.View] = Success(Row(
            access: "VIEW",
            includedDataContainers: "ALL",
            excludedDataContainers: "NONE",
            includedFields: "ALL",
            excludedFields: "NONE",
            maskedFields: "secret"));
        source.Results[AxpertPermissionKind.Edit] = Success(Row(
            access: "EDIT",
            includedDataContainers: "dc1",
            excludedDataContainers: "NONE",
            includedFields: "editable",
            excludedFields: "NONE"));
        var provider = CreateProvider(source);

        var permission = await provider.GetQueryPermissionAsync(
            "tenant-a", "dtchk", new AxRecordKey("key", "value", 10), User, default);

        Assert.True(permission.IsAllowed);
        Assert.True(permission.CanEditTransaction);
        Assert.Contains("secret", permission.ExcludedFields["dc1"]);
        Assert.Contains("dc2", permission.ReadOnlyDataContainers);
        Assert.Contains("visible", permission.ReadOnlyFields["dc1"]);
        Assert.Contains("set", permission.AllowedOperations);
    }

    [Fact]
    public async Task Edit_permission_preserves_dc_and_field_restrictions()
    {
        var source = new FakePermissionSource();
        source.Results[AxpertPermissionKind.Edit] = Success(Row(
            access: "EDIT",
            includedDataContainers: "1",
            excludedDataContainers: "2",
            includedFields: "editable",
            excludedFields: "secret"));
        var provider = CreateProvider(source);

        var permission = await provider.GetSetPermissionAsync(
            "tenant-a", "dtchk", new AxRecordKey("key", "value", 10), User, default);

        Assert.True(permission.IsAllowed);
        Assert.Contains("dc1", permission.IncludedDataContainers);
        Assert.Contains("dc2", permission.ExcludedDataContainers);
        Assert.Contains("editable", permission.IncludedFields["dc1"]);
        Assert.Contains("secret", permission.ExcludedFields["dc1"]);
    }

    [Fact]
    public async Task Cross_tenant_identity_denies_without_querying_Axpert()
    {
        var source = new FakePermissionSource();
        var provider = CreateProvider(source);
        var otherTenant = User with { TenantId = "tenant-b" };

        var permission = await provider.GetQueryPermissionAsync(
            "tenant-a", "dtchk", new AxRecordKey("key", "value", 10), otherTenant, default);

        Assert.False(permission.IsAllowed);
        Assert.Equal(0, source.CallCount);
    }

    [Fact]
    public async Task Permission_source_failure_denies()
    {
        var source = new FakePermissionSource();
        source.Results[AxpertPermissionKind.View] = new(false, [], "unavailable");
        var provider = CreateProvider(source);

        var permission = await provider.GetQueryPermissionAsync(
            "tenant-a", "dtchk", new AxRecordKey("key", "value", 10), User, default);

        Assert.False(permission.IsAllowed);
    }

    [Fact]
    public async Task New_unsaved_object_uses_create_permission()
    {
        var source = new FakePermissionSource();
        source.Results[AxpertPermissionKind.Create] = Success(Row(access: "YES"));
        var provider = CreateProvider(source);

        var permission = await provider.GetSetPermissionAsync(
            "tenant-a", "dtchk", new AxRecordKey(null, null, null), User, default);

        Assert.True(permission.IsAllowed);
        Assert.Equal([AxpertPermissionKind.Create], source.Calls);
    }

    private static AxpertAuthorizationProvider CreateProvider(FakePermissionSource source) =>
        new(source, new FakeStructDefSource());

    private static AxpertPermissionResult Success(params AxpertPermissionRow[] rows) =>
        new(true, rows);

    private static AxpertPermissionRow Row(
        string permissionType = "Role",
        string fullControl = "",
        string access = "",
        string includedDataContainers = "",
        string excludedDataContainers = "",
        string includedFields = "",
        string excludedFields = "",
        string maskedFields = "",
        string encryptedFields = "") =>
        new(
            permissionType,
            fullControl,
            access,
            includedDataContainers,
            excludedDataContainers,
            includedFields,
            excludedFields,
            maskedFields,
            encryptedFields);

    private sealed class FakePermissionSource : IAxpertPermissionSource
    {
        public Dictionary<AxpertPermissionKind, AxpertPermissionResult> Results { get; } = [];
        public List<AxpertPermissionKind> Calls { get; } = [];
        public int CallCount => Calls.Count;

        public Task<AxpertPermissionResult> LoadAsync(
            AxpertPermissionRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add(request.Kind);
            return Task.FromResult(Results.TryGetValue(request.Kind, out var result)
                ? result
                : new AxpertPermissionResult(false, [], "unscripted"));
        }
    }

    private sealed class FakeStructDefSource : IAxpertStructDefSource
    {
        public Task<AxpertStructDefSnapshot?> LoadCurrentAsync(
            string tenantId,
            string transId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<AxpertStructDefSnapshot?>(new(
                tenantId,
                transId,
                new string('a', 64),
                [
                    new("dc1", false, [new("editable"), new("visible"), new("secret")]),
                    new("dc2", true, [new("line")])
                ],
                [],
                []));
        }
    }
}
