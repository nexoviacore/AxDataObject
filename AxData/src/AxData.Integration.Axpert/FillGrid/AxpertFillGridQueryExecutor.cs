using System.Data;
using System.Globalization;
using System.Text;
using ARMCommon.Interface;
using AxData.Integration.Axpert.FieldEngine;

namespace AxData.Integration.Axpert.FillGrid;

public sealed record AxpertFillGridQueryOptions
{
    public int StatementTimeoutMilliseconds { get; init; } = 5_000;

    public int MaximumRows { get; init; } = 1_000;

    public void Validate()
    {
        if (StatementTimeoutMilliseconds <= 0)
        {
            throw new InvalidOperationException("The FillGrid statement timeout must be positive.");
        }

        if (MaximumRows <= 0 || MaximumRows == int.MaxValue)
        {
            throw new InvalidOperationException("The FillGrid row cap must be positive and bounded.");
        }
    }
}

internal sealed class AxpertFillGridQueryExecutor : IAxpertFillGridQueryExecutor
{
    private readonly IDbHelper _dbHelper;
    private readonly AxpertFillGridQueryOptions _options;

    public AxpertFillGridQueryExecutor(IDbHelper dbHelper, AxpertFillGridQueryOptions options)
    {
        ArgumentNullException.ThrowIfNull(dbHelper);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _dbHelper = dbHelper;
        _options = options;
    }

    public async Task<AxpertOperationResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> ExecuteAsync(
        AxpertFillGridQuery query,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(parameters);
        cancellationToken.ThrowIfCancellationRequested();

        var prepared = FillGridSqlPolicy.Prepare(query.SqlText, query.ParameterNames);
        if (!prepared.Succeeded || prepared.Value is null)
        {
            return Failure(prepared.Error ?? "The Axpert FillGrid query is invalid.");
        }

        var values = new object?[prepared.Value.ParameterNames.Count];
        for (var index = 0; index < prepared.Value.ParameterNames.Count; index++)
        {
            var name = prepared.Value.ParameterNames[index];
            if (!TryGetValue(parameters, name, out var value))
            {
                return Failure("The Axpert FillGrid query parameters are incomplete.");
            }

            values[index] = NormalizeParameter(value);
        }

        try
        {
            var timeout = await _dbHelper.ExecuteNonQueryAsync(
                $"SET statement_timeout = {_options.StatementTimeoutMilliseconds.ToString(CultureInfo.InvariantCulture)}")
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(timeout.error))
            {
                return Failure("The Axpert FillGrid statement timeout could not be applied.");
            }

            var cappedSql = $"""
                SELECT *
                FROM (
                {prepared.Value.SqlText}
                ) AS axdata_fillgrid
                LIMIT {(_options.MaximumRows + 1).ToString(CultureInfo.InvariantCulture)}
                """;
            var result = await _dbHelper.ExecuteSQLAsync(
                cappedSql,
                prepared.Value.ParameterNames.Select(name => $"@{name}").ToArray(),
                values.Select(GetDbType).ToArray(),
                values.Select(value => value ?? DBNull.Value).ToArray()).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(result.error))
            {
                return Failure("The Axpert FillGrid query failed.");
            }

            if (result.data.Rows.Count > _options.MaximumRows)
            {
                return Failure("The Axpert FillGrid query exceeded the configured row cap.");
            }

            var rows = result.data.Rows.Cast<DataRow>()
                .Select(row => (IReadOnlyDictionary<string, object?>)result.data.Columns.Cast<DataColumn>()
                    .ToDictionary(
                        column => column.ColumnName,
                        column => row[column] is DBNull ? null : row[column],
                        StringComparer.OrdinalIgnoreCase))
                .ToArray();
            return AxpertOperationResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(rows);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return Failure("The Axpert FillGrid query failed.");
        }
    }

    private static bool TryGetValue(
        IReadOnlyDictionary<string, object?> values,
        string name,
        out object? value)
    {
        foreach (var pair in values)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static object? NormalizeParameter(object? value) => value switch
    {
        null => null,
        byte or sbyte or short or ushort => Convert.ToInt32(value, CultureInfo.InvariantCulture),
        uint => Convert.ToInt64(value, CultureInfo.InvariantCulture),
        ulong number when number <= long.MaxValue => Convert.ToInt64(number, CultureInfo.InvariantCulture),
        float or double => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
        DateTimeOffset date => date.UtcDateTime,
        string or int or long or decimal or DateTime or bool or Guid or byte[] => value,
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)
    };

    private static DbType GetDbType(object? value) => value switch
    {
        null => DbType.String,
        int => DbType.Int32,
        long => DbType.Int64,
        decimal => DbType.Decimal,
        DateTime => DbType.DateTime,
        bool => DbType.Boolean,
        Guid => DbType.Guid,
        byte[] => DbType.Binary,
        _ => DbType.String
    };

    private static AxpertOperationResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>> Failure(
        string error) => AxpertOperationResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Failure(error);
}

internal sealed record PreparedFillGridSql(string SqlText, IReadOnlyList<string> ParameterNames);

internal static class FillGridSqlPolicy
{
    private static readonly HashSet<string> ForbiddenTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "ALTER", "ANALYZE", "CALL", "COMMENT", "COPY", "CREATE", "DELETE", "DISCARD", "DO",
        "DROP", "GRANT", "INSERT", "INTO", "LISTEN", "LOCK", "MERGE", "NOTIFY", "REFRESH",
        "RESET", "REVOKE", "SET", "TRUNCATE", "UNLISTEN", "UPDATE", "VACUUM"
    };

    public static AxpertOperationResult<PreparedFillGridSql> Prepare(
        string sql,
        IReadOnlyList<string> declaredParameterNames)
    {
        if (string.IsNullOrWhiteSpace(sql) || declaredParameterNames is null)
        {
            return Failure("The Axpert FillGrid query is empty.");
        }

        var trimmed = sql.Trim();
        if (trimmed.EndsWith(';'))
        {
            trimmed = trimmed[..^1].TrimEnd();
        }

        var declared = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawName in declaredParameterNames)
        {
            var name = rawName.Trim().TrimStart('@', ':');
            if (!IsIdentifier(name))
            {
                return Failure("The Axpert FillGrid declares an invalid parameter name.");
            }

            declared.TryAdd(name, name);
        }

        var rewritten = new StringBuilder(trimmed.Length);
        var tokens = new List<string>();
        var placeholders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < trimmed.Length;)
        {
            var current = trimmed[index];
            if (current == '\'' || current == '"')
            {
                if (!CopyQuoted(trimmed, rewritten, ref index, current))
                {
                    return Failure("The Axpert FillGrid query contains an unterminated quoted value.");
                }

                continue;
            }

            if (current == '-' && index + 1 < trimmed.Length && trimmed[index + 1] == '-')
            {
                var end = trimmed.IndexOf('\n', index + 2);
                if (end < 0)
                {
                    rewritten.Append(trimmed, index, trimmed.Length - index);
                    index = trimmed.Length;
                }
                else
                {
                    rewritten.Append(trimmed, index, end - index + 1);
                    index = end + 1;
                }

                continue;
            }

            if (current == '/' && index + 1 < trimmed.Length && trimmed[index + 1] == '*')
            {
                if (!CopyBlockComment(trimmed, rewritten, ref index))
                {
                    return Failure("The Axpert FillGrid query contains an unterminated comment.");
                }

                continue;
            }

            if (current == '$' && TryGetDollarQuoteDelimiter(trimmed, index, out var delimiter))
            {
                var end = trimmed.IndexOf(delimiter, index + delimiter.Length, StringComparison.Ordinal);
                if (end < 0)
                {
                    return Failure("The Axpert FillGrid query contains an unterminated dollar-quoted value.");
                }

                var length = end + delimiter.Length - index;
                rewritten.Append(trimmed, index, length);
                index += length;
                continue;
            }

            if (current == '$' && index + 1 < trimmed.Length && char.IsDigit(trimmed[index + 1]))
            {
                return Failure("The Axpert FillGrid query must use named parameters.");
            }

            if (current == ';' || current is '{' or '}')
            {
                return Failure("The Axpert FillGrid query contains unsupported dynamic or multi-statement SQL.");
            }

            if (current == ':' && index + 1 < trimmed.Length && trimmed[index + 1] == ':')
            {
                rewritten.Append("::");
                index += 2;
                continue;
            }

            if ((current == ':' || current == '@') &&
                index + 1 < trimmed.Length &&
                IsIdentifierStart(trimmed[index + 1]))
            {
                var start = index + 1;
                var end = start + 1;
                while (end < trimmed.Length && IsIdentifierPart(trimmed[end]))
                {
                    end++;
                }

                var name = trimmed[start..end];
                var normalizedName = declared.TryGetValue(name, out var canonicalName)
                    ? canonicalName
                    : name;
                placeholders.TryAdd(normalizedName, normalizedName);
                rewritten.Append('@').Append(normalizedName);
                index = end;
                continue;
            }

            if (IsIdentifierStart(current))
            {
                var end = index + 1;
                while (end < trimmed.Length && IsIdentifierPart(trimmed[end]))
                {
                    end++;
                }

                var token = trimmed[index..end];
                tokens.Add(token);
                rewritten.Append(token);
                index = end;
                continue;
            }

            rewritten.Append(current);
            index++;
        }

        if (tokens.Count == 0 ||
            (!string.Equals(tokens[0], "SELECT", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(tokens[0], "WITH", StringComparison.OrdinalIgnoreCase)) ||
            tokens.Any(ForbiddenTokens.Contains))
        {
            return Failure("The Axpert FillGrid query must be a read-only SELECT.");
        }

        if (placeholders.Keys.Any(name => !declared.ContainsKey(name)) ||
            declared.Keys.Any(name => !placeholders.ContainsKey(name)))
        {
            return Failure("The Axpert FillGrid SQL parameters do not match its metadata.");
        }

        var orderedNames = declaredParameterNames
            .Select(name => name.Trim().TrimStart('@', ':'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return AxpertOperationResult<PreparedFillGridSql>.Success(
            new PreparedFillGridSql(rewritten.ToString(), orderedNames));
    }

    private static bool CopyQuoted(string sql, StringBuilder output, ref int index, char quote)
    {
        output.Append(quote);
        index++;
        while (index < sql.Length)
        {
            var current = sql[index++];
            output.Append(current);
            if (current != quote)
            {
                continue;
            }

            if (index < sql.Length && sql[index] == quote)
            {
                output.Append(sql[index++]);
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool CopyBlockComment(string sql, StringBuilder output, ref int index)
    {
        var depth = 0;
        while (index < sql.Length - 1)
        {
            if (sql[index] == '/' && sql[index + 1] == '*')
            {
                depth++;
                output.Append("/*");
                index += 2;
                continue;
            }

            if (sql[index] == '*' && sql[index + 1] == '/')
            {
                depth--;
                output.Append("*/");
                index += 2;
                if (depth == 0)
                {
                    return true;
                }

                continue;
            }

            output.Append(sql[index++]);
        }

        return false;
    }

    private static bool TryGetDollarQuoteDelimiter(string sql, int index, out string delimiter)
    {
        var end = index + 1;
        while (end < sql.Length && (char.IsLetterOrDigit(sql[end]) || sql[end] == '_'))
        {
            end++;
        }

        if (end < sql.Length && sql[end] == '$')
        {
            delimiter = sql[index..(end + 1)];
            return true;
        }

        delimiter = string.Empty;
        return false;
    }

    private static bool IsIdentifier(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        IsIdentifierStart(value[0]) &&
        value.Skip(1).All(IsIdentifierPart);

    private static bool IsIdentifierStart(char value) => char.IsLetter(value) || value == '_';

    private static bool IsIdentifierPart(char value) => char.IsLetterOrDigit(value) || value is '_' or '$';

    private static AxpertOperationResult<PreparedFillGridSql> Failure(string error) =>
        AxpertOperationResult<PreparedFillGridSql>.Failure(error);
}
