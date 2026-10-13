using System.Data;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ARMCommon.Model;
using AxData.Application.Ports.Axpert;
using AxData.Integration.Axpert.Runtime;
using AxStructDef.Models;
using LegacyStructDef = AxStructDef.StructDef;

namespace AxData.Integration.Axpert.StructDef;

public interface IAxpertStructDefSource
{
    Task<AxpertStructDefSnapshot?> LoadCurrentAsync(
        string tenantId,
        string transId,
        CancellationToken cancellationToken);
}

public sealed record AxpertStructDefSnapshot(
    string TenantId,
    string TransId,
    string ContentHash,
    IReadOnlyList<DataContainerDefinition> DataContainers,
    IReadOnlyList<FieldDependency> Dependencies,
    IReadOnlyList<FillGridDefinition> FillGrids,
    string? UpdatedOn = null,
    string? LegacyVersionNo = null,
    bool RunTimeStruct = false,
    bool RunTimeMode = false);

public sealed class AxpertStructDefSource(IAxpertRuntimeContextFactory contextFactory) : IAxpertStructDefSource
{
    private static readonly Regex SafeSchemaName = new(
        "^[A-Za-z_][A-Za-z0-9_$]*$",
        RegexOptions.CultureInvariant);

    public async Task<AxpertStructDefSnapshot?> LoadCurrentAsync(
        string tenantId,
        string transId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transId);

        var runtimeResult = await contextFactory.CreateAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (!runtimeResult.Succeeded || runtimeResult.Value is null)
        {
            return null;
        }

        var runtime = runtimeResult.Value;
        var schema = NormalizeSchema(runtime.Common.GetSchemaName());
        if (!string.Equals(schema, tenantId, StringComparison.OrdinalIgnoreCase) ||
            !SafeSchemaName.IsMatch(schema))
        {
            return null;
        }

        var rowResult = await runtime.Common._dbHelper.ExecuteSQLAsync(
            "SELECT props, blobno, updatedon, runtimetstruct, runtimemod " +
            $"FROM \"{schema}\".tstructs WHERE LOWER(name) = @name ORDER BY blobno",
            ["@name"],
            [DbType.String],
            [transId.ToLowerInvariant()]).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(rowResult.error) || rowResult.data.Rows.Count == 0)
        {
            return null;
        }

        var contentHash = ComputeContentHash(rowResult);
        if (contentHash is null)
        {
            return null;
        }

        var definition = await LegacyStructDef.LoadStructure(
            runtime.Common,
            transId,
            runtime.Roles.ToList(),
            runtime.TraceService,
            refreshCache: true).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (definition?.Frames is null || definition.Flds is null || definition.Fgs is null)
        {
            return null;
        }

        var first = rowResult.data.Rows[0];
        return new AxpertStructDefSnapshot(
            tenantId,
            transId,
            contentHash,
            ProjectContainers(definition),
            ProjectDependencies(definition),
            ProjectFillGrids(definition),
            Text(first, "updatedon"),
            definition.Struct?.VersionNo,
            definition.Struct?.RunTimeStruct ?? Flag(first, "runtimetstruct"),
            definition.Struct?.RunTimeMode ?? Flag(first, "runtimemod"));
    }

    internal static string? ComputeContentHash(SQLResult result)
    {
        if (result.data.Rows.Count == 0 || !result.data.Columns.Contains("props"))
        {
            return null;
        }

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (DataRow row in result.data.Rows)
        {
            var bytes = ToBytes(row["props"]);
            if (bytes is null)
            {
                return null;
            }

            hash.AppendData(bytes);
        }

        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    private static IReadOnlyList<DataContainerDefinition> ProjectContainers(LegacyStructDef definition) =>
        definition.Frames!
            .OrderBy(frame => frame.FrameNo)
            .Select(frame =>
            {
                var fields = definition.Flds!
                    .Where(field => field.FrameNo == frame.FrameNo)
                    .OrderBy(field => field.OrderNo)
                    .ToArray();
                return new DataContainerDefinition(
                    $"dc{frame.FrameNo}",
                    frame.AsGrid,
                    fields.Select(field => new FieldDefinition(
                        field.FieldName,
                        frame.ReadOnly || field.Readonly)).ToArray(),
                    fields.Select(field => field.FieldName).ToArray());
            })
            .ToArray();

    private static IReadOnlyList<FieldDependency> ProjectDependencies(LegacyStructDef definition)
    {
        var fields = definition.Flds!;
        var dependencies = new List<FieldDependency>();
        foreach (var target in fields.Where(field => !string.IsNullOrWhiteSpace(field.Cexp)))
        {
            var declaredSources = target.Deps.Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
            var sources = declaredSources.Length > 0
                ? fields.Where(field => declaredSources.Contains(
                    field.FieldName,
                    StringComparer.OrdinalIgnoreCase))
                : fields.Where(field => ReferencesField(target.Cexp, field.FieldName));
            foreach (var source in sources)
            {
                dependencies.Add(new FieldDependency(
                    $"dc{source.FrameNo}",
                    source.FieldName,
                    $"dc{target.FrameNo}",
                    target.FieldName,
                    target.Cexp));
            }
        }

        return dependencies;
    }

    private static bool ReferencesField(string expression, string fieldName) =>
        Regex.IsMatch(
            expression,
            $"(?<![A-Za-z0-9_]){Regex.Escape(fieldName)}(?![A-Za-z0-9_])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static IReadOnlyList<FillGridDefinition> ProjectFillGrids(LegacyStructDef definition) =>
        definition.Fgs!
            .Select(fillGrid => new FillGridDefinition(
                string.IsNullOrWhiteSpace(fillGrid.Fname) ? fillGrid.Name : fillGrid.Fname,
                fillGrid.TargetDcName,
                SplitNames(fillGrid.ParamNames),
                MapFillGridMode(fillGrid.AddRows)))
            .ToArray();

    private static FillGridMode MapFillGridMode(int addRows) => addRows switch
    {
        1 or 3 => FillGridMode.Append,
        2 => FillGridMode.Replace,
        _ => throw new InvalidOperationException("The Axpert FillGrid AddRows value is unsupported.")
    };

    private static IReadOnlyList<string> SplitNames(string names) =>
        names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(name => name.TrimStart('@', ':'))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    internal static byte[]? ToBytes(object value)
    {
        if (value is null or DBNull)
        {
            return null;
        }

        if (value is byte[] bytes)
        {
            return bytes;
        }

        if (value is string text && text.StartsWith("\\x", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return Convert.FromHexString(text[2..]);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        return value is string raw ? System.Text.Encoding.UTF8.GetBytes(raw) : null;
    }

    private static string? Text(DataRow row, string column) =>
        row.Table.Columns.Contains(column) && row[column] is not DBNull
            ? Convert.ToString(row[column], System.Globalization.CultureInfo.InvariantCulture)
            : null;

    private static bool Flag(DataRow row, string column) =>
        string.Equals(Text(row, column), "Y", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeSchema(string value) => value.Split('~', 2)[0];
}
