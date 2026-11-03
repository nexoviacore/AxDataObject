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
    bool RunTimeMode = false,
    bool RequiresCompatibilityFallback = false,
    IReadOnlyList<FieldDependencyDiagnostic>? DependencyDiagnostics = null);

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

        var dependencyProjection = ProjectDependencyMetadata(definition.DependencyMetadata);
        var first = rowResult.data.Rows[0];
        return new AxpertStructDefSnapshot(
            tenantId,
            transId,
            contentHash,
            ProjectContainers(definition, dependencyProjection.Dependencies),
            dependencyProjection.Dependencies,
            ProjectFillGrids(definition),
            Text(first, "updatedon"),
            definition.Struct?.VersionNo,
            definition.Struct?.RunTimeStruct ?? Flag(first, "runtimetstruct"),
            definition.Struct?.RunTimeMode ?? Flag(first, "runtimemod"),
            dependencyProjection.RequiresCompatibilityFallback,
            dependencyProjection.Diagnostics);
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

    private static IReadOnlyList<DataContainerDefinition> ProjectContainers(
        LegacyStructDef definition,
        IReadOnlyList<FieldDependency> dependencies) =>
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
                    DeriveEvaluationOrder(
                        frame.FrameNo,
                        fields.Select(field => field.FieldName).ToArray(),
                        dependencies));
            })
            .ToArray();

    internal static DependencyProjection ProjectDependencyMetadata(TDependencyMetadata? metadata)
    {
        if (metadata is null)
        {
            return new DependencyProjection(
                [],
                true,
                [new FieldDependencyDiagnostic(
                    FieldDependencyDiagnosticKind.MalformedMetadata,
                    string.Empty,
                    string.Empty,
                    "Authoritative ARM dependency metadata is unavailable.")]);
        }

        var dependencies = metadata.Edges
            .OrderBy(edge => edge.DeclarationOrder)
            .Select(edge => new FieldDependency(
                $"dc{edge.SourceFrameNo}",
                edge.Source,
                edge.TargetFrameNo is int targetFrameNo ? $"dc{targetFrameNo}" : null,
                edge.Target,
                MapDependencyKind(edge.Kind),
                edge.DeclarationOrder,
                edge.SourceFrameNo,
                edge.TargetFrameNo,
                edge.SourceIsGrid,
                edge.TargetIsGrid,
                edge.TargetIsPopup,
                edge.IsResolved))
            .ToArray();
        var diagnostics = metadata.Diagnostics
            .Select(diagnostic => new FieldDependencyDiagnostic(
                MapDiagnosticKind(diagnostic.Kind),
                diagnostic.Source,
                diagnostic.Target,
                diagnostic.Message))
            .ToArray();

        return new DependencyProjection(
            dependencies,
            metadata.RequiresCompatibilityFallback,
            diagnostics);
    }

    internal static IReadOnlyList<string> DeriveEvaluationOrder(
        int frameNo,
        IReadOnlyList<string> fieldsInDisplayOrder,
        IReadOnlyList<FieldDependency> dependencies)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var fieldOrder = fieldsInDisplayOrder
            .Select((field, index) => (field, index))
            .ToDictionary(item => item.field, item => item.index, comparer);
        var ordinaryEdges = dependencies
            .Where(dependency =>
                dependency.IsResolved &&
                dependency.SourceFrameNo == frameNo &&
                dependency.TargetFrameNo == frameNo &&
                IsOrdinary(dependency.Kind) &&
                fieldOrder.ContainsKey(dependency.SourceField) &&
                fieldOrder.ContainsKey(dependency.TargetField))
            .OrderBy(dependency => dependency.DeclarationOrder)
            .ToArray();
        var outgoing = fieldsInDisplayOrder.ToDictionary(
            field => field,
            _ => new List<FieldDependency>(),
            comparer);
        var indegree = fieldsInDisplayOrder.ToDictionary(field => field, _ => 0, comparer);
        var metadataOrder = fieldsInDisplayOrder.ToDictionary(field => field, _ => int.MaxValue, comparer);

        foreach (var edge in ordinaryEdges)
        {
            if (outgoing[edge.SourceField].Any(existing =>
                    comparer.Equals(existing.TargetField, edge.TargetField)))
            {
                continue;
            }

            outgoing[edge.SourceField].Add(edge);
            indegree[edge.TargetField]++;
            metadataOrder[edge.SourceField] = Math.Min(metadataOrder[edge.SourceField], edge.DeclarationOrder);
            metadataOrder[edge.TargetField] = Math.Min(metadataOrder[edge.TargetField], edge.DeclarationOrder);
        }

        var ready = fieldsInDisplayOrder
            .Where(field => indegree[field] == 0)
            .ToList();
        var result = new List<string>(fieldsInDisplayOrder.Count);
        while (ready.Count > 0)
        {
            ready.Sort((left, right) =>
            {
                var metadataComparison = metadataOrder[left].CompareTo(metadataOrder[right]);
                return metadataComparison != 0
                    ? metadataComparison
                    : fieldOrder[left].CompareTo(fieldOrder[right]);
            });
            var source = ready[0];
            ready.RemoveAt(0);
            result.Add(source);

            foreach (var edge in outgoing[source].OrderBy(edge => edge.DeclarationOrder))
            {
                indegree[edge.TargetField]--;
                if (indegree[edge.TargetField] == 0)
                {
                    ready.Add(edge.TargetField);
                }
            }
        }

        result.AddRange(fieldsInDisplayOrder.Where(field => !result.Contains(field, comparer)));
        return result;
    }

    private static bool IsOrdinary(FieldDependencyKind kind) => kind is
        FieldDependencyKind.Expression or
        FieldDependencyKind.Select or
        FieldDependencyKind.Fill or
        FieldDependencyKind.Aggregate;

    private static FieldDependencyKind MapDependencyKind(TDependencyKind kind) => kind switch
    {
        TDependencyKind.Expression => FieldDependencyKind.Expression,
        TDependencyKind.Select => FieldDependencyKind.Select,
        TDependencyKind.Fill => FieldDependencyKind.Fill,
        TDependencyKind.Aggregate => FieldDependencyKind.Aggregate,
        TDependencyKind.FillGrid => FieldDependencyKind.FillGrid,
        TDependencyKind.DataContainer => FieldDependencyKind.DataContainer,
        TDependencyKind.Label => FieldDependencyKind.Label,
        TDependencyKind.DependentFrame => FieldDependencyKind.DependentFrame,
        TDependencyKind.Grid => FieldDependencyKind.Grid,
        _ => throw new InvalidOperationException("The ARM dependency kind is unsupported.")
    };

    private static FieldDependencyDiagnosticKind MapDiagnosticKind(TDependencyDiagnosticKind kind) => kind switch
    {
        TDependencyDiagnosticKind.MalformedMetadata => FieldDependencyDiagnosticKind.MalformedMetadata,
        TDependencyDiagnosticKind.UnresolvedReference => FieldDependencyDiagnosticKind.UnresolvedReference,
        TDependencyDiagnosticKind.Cycle => FieldDependencyDiagnosticKind.Cycle,
        _ => throw new InvalidOperationException("The ARM dependency diagnostic kind is unsupported.")
    };

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

    internal sealed record DependencyProjection(
        IReadOnlyList<FieldDependency> Dependencies,
        bool RequiresCompatibilityFallback,
        IReadOnlyList<FieldDependencyDiagnostic> Diagnostics);
}
