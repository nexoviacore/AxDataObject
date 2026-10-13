using ARMCommon.Helpers;
using ARMCommon.Interface;
using AxData.Integration.Axpert.FieldEngine;
using System.Text.RegularExpressions;
using static ARMCommon.Helpers.Constants;

namespace AxData.Integration.Axpert.Runtime;

public sealed record AxpertReviewRuntimeOptions
{
    public required string TenantId { get; init; }
    public required string SessionId { get; init; }
    public required string UserName { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public required string DatabaseType { get; init; }
    public required string ConnectionString { get; init; }
    public string? AxpertSessionId { get; init; }
}

public sealed record AxpertRuntimeContext(
    Utils Common,
    IARMLogTraceService TraceService,
    IReadOnlyList<string> Roles,
    string SessionId);

public interface IAxpertRuntimeContextFactory
{
    Task<AxpertOperationResult<AxpertRuntimeContext>> CreateAsync(
        string tenantId,
        CancellationToken cancellationToken);
}

public sealed class AxpertRuntimeContextFactory(
    IARMLogTraceService traceService,
    AxpertReviewRuntimeOptions options) : IAxpertRuntimeContextFactory
{
    private static readonly Regex SafeSchemaName = new(
        "^[A-Za-z_][A-Za-z0-9_$]*$",
        RegexOptions.CultureInvariant);

    public async Task<AxpertOperationResult<AxpertRuntimeContext>> CreateAsync(
        string tenantId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Same(tenantId, options.TenantId) ||
            string.IsNullOrWhiteSpace(options.SessionId) ||
            !options.SessionId.StartsWith($"ARM-{tenantId}-", StringComparison.OrdinalIgnoreCase))
        {
            return Failure("The configured Axpert review session does not match the requested tenant.");
        }

        try
        {
            var common = CreateCommon(tenantId);
            common._appName = tenantId;
            var session = await common.GetSessionData(options.SessionId).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (session.Count == 0 ||
                !TryGet(session, SESSION_DATA.USERNAME.ToString(), out var sessionUser) ||
                !Same(sessionUser, options.UserName))
            {
                return Failure("The configured Axpert review session is missing or belongs to another user.");
            }

            await common._dbHelper.OpenDBConnectionAsync().ConfigureAwait(false);
            var readOnlyResult = await common._dbHelper.ExecuteNonQueryAsync(
                "SET default_transaction_read_only = on").ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(readOnlyResult.error))
            {
                return Failure("The Axpert review database session could not be made read-only.");
            }

            var schemaResult = await common._dbHelper.ExecuteNonQueryAsync(
                $"SET search_path TO \"{tenantId}\"").ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(schemaResult.error))
            {
                return Failure("The Axpert review tenant schema could not be selected.");
            }

            var schema = NormalizeSchema(common.GetSchemaName());
            if (!Same(schema, tenantId))
            {
                return Failure("The Axpert database schema does not match the requested tenant.");
            }

            return new AxpertOperationResult<AxpertRuntimeContext>(
                true,
                new AxpertRuntimeContext(common, traceService, options.Roles.ToArray(), options.SessionId),
                null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Failure("The Axpert review runtime could not be initialized.");
        }
    }

    private Utils CreateCommon(string tenantId)
    {
        if (!SafeSchemaName.IsMatch(tenantId) ||
            !Same(options.DatabaseType, DBTYPE.POSTGRE.ToString()) ||
            string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("The Axpert review database configuration is invalid.");
        }

        var common = new Utils
        {
            _appName = tenantId,
            _armSessionId = options.SessionId,
            _schemaName = tenantId,
            _schemaNameALC = tenantId,
            _dbType = options.DatabaseType,
            _redisCache = NoOpRedisCache.Create(),
            _dbHelper = DBHelper.CreateDbHelper(options.DatabaseType, traceService)
        };
        common._dbHelper.ConnectionString = options.ConnectionString;
        common._dbVars = common.GetDBVarsList(options.DatabaseType);
        common._sessionVars = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            [SESSION_DATA.USERNAME.ToString()] = options.UserName,
            [SESSION_DATA.USER_ROLES.ToString()] = string.Join(',', options.Roles),
            [SESSION_DATA.APPNAME.ToString()] = tenantId,
            [SESSION_DATA.AXPERT_SESSIONID.ToString()] = options.AxpertSessionId ?? options.SessionId,
            [SESSION_DATA.LANGUAGE.ToString()] = "eng",
            [SESSION_DATA.SITENO.ToString()] = "0"
        };

        return common;
    }

    private static AxpertOperationResult<AxpertRuntimeContext> Failure(string error) =>
        new(false, null!, error);

    private static bool TryGet(
        IReadOnlyDictionary<string, object> values,
        string name,
        out string value)
    {
        foreach (var pair in values)
        {
            if (Same(pair.Key, name))
            {
                value = Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static string NormalizeSchema(string value) => value.Split('~', 2)[0];

    private static bool Same(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
