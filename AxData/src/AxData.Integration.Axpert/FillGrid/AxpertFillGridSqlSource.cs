using System.Data;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml;
using AxData.Integration.Axpert.FieldEngine;
using AxData.Integration.Axpert.Runtime;
using AxData.Integration.Axpert.StructDef;

namespace AxData.Integration.Axpert.FillGrid;

internal static partial class AxpertFillGridSqlSource
{
    private const int MaximumStructDefBytes = 64 * 1024 * 1024;

    public static async Task<AxpertOperationResult<IReadOnlyDictionary<string, string>>> LoadAsync(
        AxpertRuntimeContext context,
        string tenantId,
        string transId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transId);

        var schema = context.Common.GetSchemaName().Split('~', 2)[0];
        if (!string.Equals(schema, tenantId, StringComparison.OrdinalIgnoreCase) ||
            !SafeSchemaName().IsMatch(schema))
        {
            return Failure("The FillGrid StructDef tenant schema is invalid.");
        }

        var result = await context.Common._dbHelper.ExecuteSQLAsync(
            "SELECT props " +
            $"FROM \"{schema}\".tstructs WHERE LOWER(name) = @name ORDER BY blobno",
            ["@name"],
            [DbType.String],
            [transId.ToLowerInvariant()]).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(result.error) || result.data.Rows.Count == 0)
        {
            return Failure("The FillGrid StructDef XML could not be loaded.");
        }

        var chunks = new List<byte[]>(result.data.Rows.Count);
        foreach (DataRow row in result.data.Rows)
        {
            var bytes = AxpertStructDefSource.ToBytes(row["props"]);
            if (bytes is null)
            {
                return Failure("The FillGrid StructDef XML has an invalid payload.");
            }

            chunks.Add(bytes);
        }

        return await ParseAsync(chunks, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<AxpertOperationResult<IReadOnlyDictionary<string, string>>> ParseAsync(
        IEnumerable<byte[]> chunks,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var compressed = new MemoryStream();
            foreach (var chunk in chunks)
            {
                await compressed.WriteAsync(chunk, cancellationToken).ConfigureAwait(false);
            }

            compressed.Position = 0;
            var sizeBytes = new byte[sizeof(uint)];
            if (await compressed.ReadAsync(sizeBytes, cancellationToken).ConfigureAwait(false) != sizeBytes.Length)
            {
                return Failure("The FillGrid StructDef XML has an invalid compression header.");
            }

            var expectedLength = BitConverter.ToUInt32(sizeBytes, 0);
            if (expectedLength == 0 || expectedLength > MaximumStructDefBytes)
            {
                return Failure("The FillGrid StructDef XML exceeds the allowed size.");
            }

            await using var decompressed = new MemoryStream((int)expectedLength);
            await using (var zlib = new ZLibStream(compressed, CompressionMode.Decompress, leaveOpen: true))
            {
                await zlib.CopyToAsync(decompressed, cancellationToken).ConfigureAwait(false);
            }

            if (decompressed.Length != expectedLength)
            {
                return Failure("The FillGrid StructDef XML length is invalid.");
            }

            decompressed.Position = 0;
            var settings = new XmlReaderSettings
            {
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaximumStructDefBytes
            };
            using var reader = XmlReader.Create(decompressed, settings);
            var document = new XmlDocument { XmlResolver = null };
            document.Load(reader);

            var queries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var root = document.DocumentElement;
            if (root is null)
            {
                return Failure("The FillGrid StructDef XML has no document element.");
            }

            foreach (XmlNode node in root.ChildNodes)
            {
                if (!string.Equals(node.Attributes?["cat"]?.Value, "fillgrid", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var sqlNode = node.SelectSingleNode("a5");
                var sqlText = ReadSql(sqlNode);
                if (string.IsNullOrWhiteSpace(node.Name) || string.IsNullOrWhiteSpace(sqlText) ||
                    !queries.TryAdd(node.Name, sqlText))
                {
                    return Failure("The FillGrid StructDef XML contains missing or duplicate query metadata.");
                }
            }

            return AxpertOperationResult<IReadOnlyDictionary<string, string>>.Success(queries);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidDataException)
        {
            return Failure("The FillGrid StructDef XML compression is invalid.");
        }
        catch (XmlException)
        {
            return Failure("The FillGrid StructDef XML is invalid.");
        }
    }

    private static string ReadSql(XmlNode? node)
    {
        if (node is null)
        {
            return string.Empty;
        }

        if (!node.HasChildNodes)
        {
            return node.InnerText.Trim();
        }

        return string.Join(
            Environment.NewLine,
            node.ChildNodes.Cast<XmlNode>().Select(child => child.InnerText)).Trim();
    }

    private static AxpertOperationResult<IReadOnlyDictionary<string, string>> Failure(string error) =>
        AxpertOperationResult<IReadOnlyDictionary<string, string>>.Failure(error);

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_$]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeSchemaName();
}
