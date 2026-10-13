using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Integration.Axpert.Runtime;
using AxData.Integration.Axpert.StructDef;
using AxTstructData;

namespace AxData.Integration.Axpert.Authorization;

public enum AxpertPermissionKind
{
    Create,
    View,
    Edit
}

public sealed record AxpertPermissionRequest(
    string TenantId,
    string TransId,
    AxRecordKey Key,
    string UserName,
    IReadOnlySet<string> Roles,
    AxpertPermissionKind Kind);

public sealed record AxpertPermissionRow(
    string PermissionType,
    string FullControl,
    string Access,
    string IncludedDataContainers,
    string ExcludedDataContainers,
    string IncludedFields,
    string ExcludedFields,
    string MaskedFields,
    string EncryptedFields);

public sealed record AxpertPermissionResult(
    bool Succeeded,
    IReadOnlyList<AxpertPermissionRow> Rows,
    string? Error = null);

public interface IAxpertPermissionSource
{
    Task<AxpertPermissionResult> LoadAsync(
        AxpertPermissionRequest request,
        CancellationToken cancellationToken);
}

public sealed class AxpertPermissionSource(
    IAxpertRuntimeContextFactory contextFactory) : IAxpertPermissionSource
{
    public async Task<AxpertPermissionResult> LoadAsync(
        AxpertPermissionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var contextResult = await contextFactory.CreateAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (!contextResult.Succeeded || contextResult.Value is null)
        {
            return Failure("The Axpert permission runtime could not be created.");
        }

        try
        {
            var context = contextResult.Value;
            var roles = request.Roles.ToList();
            var dimensions = context.Common.GetAxgApplicableDimensionsString();
            ARMCommon.Model.SQLResult result;
            switch (request.Kind)
            {
                case AxpertPermissionKind.Create:
                    result = await new AxPutRepository(context.Common, context.TraceService)
                        .GetCreatePermissionFromDB(new AxPut
                        {
                            Action = "create",
                            TransId = request.TransId,
                            UserName = request.UserName,
                            Roles = roles,
                            Dimensions = dimensions
                        }).ConfigureAwait(false);
                    break;
                case AxpertPermissionKind.View:
                    result = await new AxGetRepository(context.Common, context.TraceService)
                        .GetTstructViewPermissionFromDB(new AxTstructData.AxGet
                        {
                            Action = "view",
                            TransId = request.TransId,
                            KeyField = AxpertRecordKeyMapping.Field(request.Key),
                            KeyValue = AxpertRecordKeyMapping.Value(request.Key),
                            UserName = request.UserName,
                            Roles = roles,
                            Dimensions = dimensions
                        }).ConfigureAwait(false);
                    break;
                case AxpertPermissionKind.Edit:
                    result = await new AxPutRepository(context.Common, context.TraceService)
                        .GetEditPermissionFromDB(new AxPut
                        {
                            Action = "edit",
                            TransId = request.TransId,
                            KeyField = AxpertRecordKeyMapping.Field(request.Key),
                            KeyValue = AxpertRecordKeyMapping.Value(request.Key),
                            UserName = request.UserName,
                            Roles = roles,
                            Dimensions = dimensions
                        }).ConfigureAwait(false);
                    break;
                default:
                    return Failure("The Axpert permission kind is unsupported.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (result?.data is null || !string.IsNullOrWhiteSpace(result.error))
            {
                return Failure("The Axpert permission query failed.");
            }

            return new AxpertPermissionResult(
                true,
                result.data.Rows.Cast<System.Data.DataRow>()
                    .Select(row => Project(row, request.Kind))
                    .ToArray());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Failure("The Axpert permission query failed.");
        }
    }

    private static AxpertPermissionRow Project(
        System.Data.DataRow row,
        AxpertPermissionKind kind)
    {
        var prefix = kind == AxpertPermissionKind.View ? "view_" : "edit_";
        return new AxpertPermissionRow(
            Text(row, "permissiontype"),
            Text(row, "fullcontrol"),
            kind == AxpertPermissionKind.Create ? Text(row, "allowcreate") : Text(row, $"{prefix}access"),
            kind == AxpertPermissionKind.Create ? string.Empty : Text(row, $"{prefix}includedc"),
            kind == AxpertPermissionKind.Create ? string.Empty : Text(row, $"{prefix}excludedc"),
            kind == AxpertPermissionKind.Create ? string.Empty : Text(row, $"{prefix}includeflds"),
            kind == AxpertPermissionKind.Create ? string.Empty : Text(row, $"{prefix}excludeflds"),
            kind == AxpertPermissionKind.View ? Text(row, "maskedflds") : string.Empty,
            kind == AxpertPermissionKind.View ? Text(row, "encryptedflds") : string.Empty);
    }

    private static string Text(System.Data.DataRow row, string column) =>
        row.Table.Columns.Contains(column) && row[column] is not DBNull
            ? Convert.ToString(row[column], System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
            : string.Empty;

    private static AxpertPermissionResult Failure(string error) => new(false, [], error);
}

public sealed class AxpertAuthorizationProvider(
    IAxpertPermissionSource permissionSource,
    IAxpertStructDefSource structDefSource) : IAxpertAuthorizationProvider
{
    public async Task<AxCreatePermission> GetCreatePermissionAsync(
        string tenantId,
        string transId,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        if (!ValidIdentity(tenantId, transId, user))
        {
            return new AxCreatePermission(false);
        }

        var result = await LoadAsync(
            tenantId,
            transId,
            new AxRecordKey(null, null, null),
            user,
            AxpertPermissionKind.Create,
            cancellationToken).ConfigureAwait(false);
        return new AxCreatePermission(
            result.Succeeded && PreferredRows(result.Rows).Any(CanCreate));
    }

    public async Task<AxQueryPermission> GetQueryPermissionAsync(
        string tenantId,
        string transId,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        if (!ValidIdentity(tenantId, transId, user))
        {
            return AxQueryPermission.Denied;
        }

        if (key.RecordId is null && string.IsNullOrWhiteSpace(key.KeyField))
        {
            var create = await GetCreatePermissionAsync(tenantId, transId, user, cancellationToken)
                .ConfigureAwait(false);
            return create.IsAllowed ? Unrestricted(canEdit: true) : AxQueryPermission.Denied;
        }

        var view = await LoadAsync(
            tenantId,
            transId,
            key,
            user,
            AxpertPermissionKind.View,
            cancellationToken).ConfigureAwait(false);
        if (!view.Succeeded)
        {
            return AxQueryPermission.Denied;
        }

        var viewRows = PreferredRows(view.Rows).Where(CanViewOrEdit).ToArray();
        if (viewRows.Length == 0)
        {
            return AxQueryPermission.Denied;
        }

        var edit = await LoadAsync(
            tenantId,
            transId,
            key,
            user,
            AxpertPermissionKind.Edit,
            cancellationToken).ConfigureAwait(false);
        var editRows = edit.Succeeded
            ? PreferredRows(edit.Rows).Where(CanViewOrEdit).ToArray()
            : [];
        var definition = await structDefSource.LoadCurrentAsync(tenantId, transId, cancellationToken)
            .ConfigureAwait(false);
        if (definition is null)
        {
            return AxQueryPermission.Denied;
        }

        return BuildPermission(definition, viewRows, editRows);
    }

    public async Task<AxQueryPermission> GetSetPermissionAsync(
        string tenantId,
        string transId,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        if (!ValidIdentity(tenantId, transId, user))
        {
            return AxQueryPermission.Denied;
        }

        if (key.RecordId is null && string.IsNullOrWhiteSpace(key.KeyField))
        {
            var create = await GetCreatePermissionAsync(tenantId, transId, user, cancellationToken)
                .ConfigureAwait(false);
            return create.IsAllowed ? Unrestricted(canEdit: true) : AxQueryPermission.Denied;
        }

        var edit = await LoadAsync(
            tenantId,
            transId,
            key,
            user,
            AxpertPermissionKind.Edit,
            cancellationToken).ConfigureAwait(false);
        if (!edit.Succeeded)
        {
            return AxQueryPermission.Denied;
        }

        var rows = PreferredRows(edit.Rows).Where(CanViewOrEdit).ToArray();
        if (rows.Length == 0)
        {
            return AxQueryPermission.Denied;
        }

        var definition = await structDefSource.LoadCurrentAsync(tenantId, transId, cancellationToken)
            .ConfigureAwait(false);
        return definition is null ? AxQueryPermission.Denied : BuildSetPermission(definition, rows);
    }

    private Task<AxpertPermissionResult> LoadAsync(
        string tenantId,
        string transId,
        AxRecordKey key,
        AxUserContext user,
        AxpertPermissionKind kind,
        CancellationToken cancellationToken) =>
        permissionSource.LoadAsync(
            new AxpertPermissionRequest(
                tenantId,
                transId,
                key,
                user.UserName,
                user.Roles,
                kind),
            cancellationToken);

    private static AxQueryPermission BuildPermission(
        AxpertStructDefSnapshot definition,
        IReadOnlyCollection<AxpertPermissionRow> viewRows,
        IReadOnlyCollection<AxpertPermissionRow> editRows)
    {
        var view = Selectors.From(viewRows, includeMaskedAndEncrypted: true);
        var edit = Selectors.From(editRows, includeMaskedAndEncrypted: false);
        var canEdit = editRows.Count > 0;
        var readOnlyContainers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var readOnlyFields = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var container in definition.DataContainers)
        {
            if (Visible(container.Name, view.IncludedDataContainers, view.ExcludedDataContainers) &&
                (!canEdit || !Visible(container.Name, edit.IncludedDataContainers, edit.ExcludedDataContainers)))
            {
                readOnlyContainers.Add(container.Name);
            }

            var fields = container.Fields
                .Where(field => FieldVisible(field.Name, view.IncludedFields, view.ExcludedFields) &&
                    (!canEdit || !FieldVisible(field.Name, edit.IncludedFields, edit.ExcludedFields)))
                .Select(field => field.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (fields.Count > 0)
            {
                readOnlyFields[container.Name] = fields;
            }
        }

        return new AxQueryPermission(
            true,
            canEdit,
            view.IncludedDataContainers,
            view.ExcludedDataContainers,
            FieldSets(definition, view.IncludedFields),
            FieldSets(definition, view.ExcludedFields),
            readOnlyContainers,
            readOnlyFields,
            Set(canEdit ? ["query", "set", "save"] : ["query"]));
    }

    private static AxQueryPermission BuildSetPermission(
        AxpertStructDefSnapshot definition,
        IReadOnlyCollection<AxpertPermissionRow> rows)
    {
        var selectors = Selectors.From(rows, includeMaskedAndEncrypted: false);
        return new AxQueryPermission(
            true,
            true,
            selectors.IncludedDataContainers,
            selectors.ExcludedDataContainers,
            FieldSets(definition, selectors.IncludedFields),
            FieldSets(definition, selectors.ExcludedFields),
            EmptySet,
            EmptyFieldSets,
            Set("query", "set", "save"));
    }

    private static AxQueryPermission Unrestricted(bool canEdit) => new(
        true,
        canEdit,
        EmptySet,
        EmptySet,
        EmptyFieldSets,
        EmptyFieldSets,
        EmptySet,
        EmptyFieldSets,
        Set(canEdit ? ["query", "set", "save"] : ["query"]));

    private static IReadOnlyDictionary<string, IReadOnlySet<string>> FieldSets(
        AxpertStructDefSnapshot definition,
        IReadOnlySet<string> fields)
    {
        if (fields.Count == 0)
        {
            return EmptyFieldSets;
        }

        return definition.DataContainers.ToDictionary(
            container => container.Name,
            _ => fields,
            StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<AxpertPermissionRow> PreferredRows(
        IReadOnlyList<AxpertPermissionRow> rows)
    {
        var userRows = rows.Where(row =>
            string.Equals(row.PermissionType, "User", StringComparison.OrdinalIgnoreCase)).ToArray();
        return userRows.Length > 0 ? userRows : rows;
    }

    private static bool CanCreate(AxpertPermissionRow row) =>
        IsTrue(row.FullControl) || string.Equals(row.Access, "YES", StringComparison.OrdinalIgnoreCase);

    private static bool CanViewOrEdit(AxpertPermissionRow row) =>
        IsTrue(row.FullControl) ||
        (string.IsNullOrWhiteSpace(row.FullControl) &&
         !string.Equals(row.Access, "NONE", StringComparison.OrdinalIgnoreCase));

    private static bool IsTrue(string value) =>
        string.Equals(value, "T", StringComparison.OrdinalIgnoreCase);

    private static bool ValidIdentity(string tenantId, string transId, AxUserContext user) =>
        !string.IsNullOrWhiteSpace(tenantId) &&
        !string.IsNullOrWhiteSpace(transId) &&
        string.Equals(tenantId, user.TenantId, StringComparison.OrdinalIgnoreCase);

    private static bool Visible(string name, IReadOnlySet<string> included, IReadOnlySet<string> excluded) =>
        !excluded.Contains(name) && (included.Count == 0 || included.Contains(name));

    private static bool FieldVisible(string name, IReadOnlySet<string> included, IReadOnlySet<string> excluded) =>
        !excluded.Contains(name) && (included.Count == 0 || included.Contains(name));

    private static IReadOnlySet<string> Set(params string[] values) =>
        new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlySet<string> EmptySet = Set();

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> EmptyFieldSets =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);

    private sealed record Selectors(
        IReadOnlySet<string> IncludedDataContainers,
        IReadOnlySet<string> ExcludedDataContainers,
        IReadOnlySet<string> IncludedFields,
        IReadOnlySet<string> ExcludedFields)
    {
        public static Selectors From(
            IReadOnlyCollection<AxpertPermissionRow> rows,
            bool includeMaskedAndEncrypted)
        {
            var includedContainers = Normalize(
                rows.SelectMany(row => Tokens(row.IncludedDataContainers)),
                "ALL",
                normalizeContainer: true);
            var excludedContainers = Normalize(
                rows.SelectMany(row => Tokens(row.ExcludedDataContainers)),
                "NONE",
                normalizeContainer: true);
            var includedFields = Normalize(
                rows.SelectMany(row => Tokens(row.IncludedFields)),
                "ALL",
                normalizeContainer: false);
            var excludedFields = Normalize(
                rows.SelectMany(row => Tokens(row.ExcludedFields)),
                "NONE",
                normalizeContainer: false).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (includeMaskedAndEncrypted)
            {
                excludedFields.UnionWith(rows.SelectMany(row =>
                    Tokens(row.MaskedFields).Concat(Tokens(row.EncryptedFields))));
            }

            return new Selectors(
                includedContainers,
                excludedContainers,
                includedFields,
                excludedFields);
        }

        private static IReadOnlySet<string> Normalize(
            IEnumerable<string> values,
            string unrestrictedToken,
            bool normalizeContainer)
        {
            var tokens = values.ToArray();
            if (tokens.Any(value => string.Equals(value, unrestrictedToken, StringComparison.OrdinalIgnoreCase)))
            {
                return EmptySet;
            }

            return tokens
                .Where(value => !string.Equals(value, "NONE", StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(value, "ALL", StringComparison.OrdinalIgnoreCase))
                .Select(value => normalizeContainer && int.TryParse(value, out var number) ? $"dc{number}" : value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> Tokens(string value) =>
            value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
